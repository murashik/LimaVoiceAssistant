using FuzzySharp;
using LimaVoiceAssistant.Clients;
using LimaVoiceAssistant.Models;
using NLog;

namespace LimaVoiceAssistant.Services;

/// <summary>
/// Сервис для поиска препаратов с использованием нечёткого поиска FuzzySharp
/// </summary>
public class DrugSearchService : IDrugSearchService
{
    private readonly ILimaApiClient _limaApiClient;
    private readonly NLog.ILogger _logger;

    // Кеш для хранения данных на время сессии
    private List<PriceListItem>? _priceListCache;
    private List<CompanyDrug>? _companyDrugsCache;
    private DateTime? _priceListCacheTime;
    private DateTime? _companyDrugsCacheTime;

    // Время жизни кеша - 15 минут
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);

    public DrugSearchService(ILimaApiClient limaApiClient)
    {
        _limaApiClient = limaApiClient;
        _logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Получение прайс-листа с кешированием
    /// </summary>
    /// <returns>Актуальный прайс-лист</returns>
    private async Task<List<PriceListItem>> GetPriceListAsync()
    {
        if (_priceListCache != null && _priceListCacheTime.HasValue && 
            DateTime.UtcNow - _priceListCacheTime.Value < _cacheExpiry)
        {
            _logger.Debug("Используется кешированный прайс-лист");
            return _priceListCache;
        }

        _logger.Info("Обновление кеша прайс-листа");
        _priceListCache = await _limaApiClient.GetPriceListAsync();
        _priceListCacheTime = DateTime.UtcNow;
        
        return _priceListCache;
    }

    /// <summary>
    /// Получение списка препаратов компании с кешированием
    /// </summary>
    /// <returns>Актуальный список препаратов компании</returns>
    private async Task<List<CompanyDrug>> GetCompanyDrugsAsync()
    {
        if (_companyDrugsCache != null && _companyDrugsCacheTime.HasValue && 
            DateTime.UtcNow - _companyDrugsCacheTime.Value < _cacheExpiry)
        {
            _logger.Debug("Используется кешированный список препаратов компании");
            return _companyDrugsCache;
        }

        _logger.Info("Обновление кеша препаратов компании");
        _companyDrugsCache = await _limaApiClient.GetCompanyDrugsAsync();
        _companyDrugsCacheTime = DateTime.UtcNow;
        
        return _companyDrugsCache;
    }

    /// <summary>
    /// Нормализация названия препарата для лучшего поиска
    /// </summary>
    /// <param name="drugName">Исходное название</param>
    /// <returns>Нормализованное название</returns>
    private string NormalizeDrugName(string drugName)
    {
        if (string.IsNullOrWhiteSpace(drugName))
            return string.Empty;

        // Приводим к нижнему регистру и удаляем лишние пробелы
        return drugName.Trim().ToLowerInvariant()
            .Replace("ё", "е") // Замена ё на е для лучшего поиска
            .Replace("  ", " "); // Удаление двойных пробелов
    }

    /// <summary>
    /// Поиск препарата в прайс-листе по названию с использованием нечёткого поиска
    /// </summary>
    /// <param name="drugName">Название препарата для поиска</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 60)</param>
    /// <returns>Найденный препарат из прайс-листа или null</returns>
    public async Task<PriceListItem?> FindDrugInPriceListAsync(string drugName, int threshold = 60)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(drugName))
            {
                _logger.Warn("Пустое название препарата для поиска в прайс-листе");
                return null;
            }

            _logger.Info($"Поиск препарата в прайс-листе: '{drugName}' с порогом {threshold}");
            
            var priceList = await GetPriceListAsync();
            var normalizedSearchName = NormalizeDrugName(drugName);

            PriceListItem? bestMatch = null;
            int bestScore = 0;

            foreach (var item in priceList)
            {
                if (item.Drug?.DrugName == null) continue;

                var normalizedDrugName = NormalizeDrugName(item.Drug.DrugName);
                
                // Проверяем точное совпадение
                if (normalizedDrugName == normalizedSearchName)
                {
                    _logger.Info($"Найдено точное совпадение: '{item.Drug.DrugName}'");
                    return item;
                }

                // Проверяем содержание подстроки
                if (normalizedDrugName.Contains(normalizedSearchName) || normalizedSearchName.Contains(normalizedDrugName))
                {
                    _logger.Info($"Найдено совпадение по подстроке: '{item.Drug.DrugName}'");
                    return item;
                }

                // Используем нечёткий поиск
                var score = Fuzz.Ratio(normalizedSearchName, normalizedDrugName);
                
                // Также проверяем частичные совпадения
                var partialScore = Fuzz.PartialRatio(normalizedSearchName, normalizedDrugName);
                var tokenScore = Fuzz.TokenSortRatio(normalizedSearchName, normalizedDrugName);
                
                // Берём максимальный из всех скоров
                var maxScore = Math.Max(score, Math.Max(partialScore, tokenScore));

                if (maxScore > bestScore && maxScore >= threshold)
                {
                    bestScore = maxScore;
                    bestMatch = item;
                }
            }

            if (bestMatch != null)
            {
                _logger.Info($"Найден препарат в прайс-листе: '{bestMatch.Drug.DrugName}' (схожесть: {bestScore}%)");
            }
            else
            {
                _logger.Warn($"Препарат '{drugName}' не найден в прайс-листе с порогом {threshold}%");
            }

            return bestMatch;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при поиске препарата '{drugName}' в прайс-листе");
            return null;
        }
    }

    /// <summary>
    /// Поиск препарата в списке препаратов компании по названию с использованием нечёткого поиска
    /// </summary>
    /// <param name="drugName">Название препарата для поиска</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 60)</param>
    /// <returns>Найденный препарат компании или null</returns>
    public async Task<CompanyDrug?> FindCompanyDrugAsync(string drugName, int threshold = 60)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(drugName))
            {
                _logger.Warn("Пустое название препарата для поиска в списке компании");
                return null;
            }

            _logger.Info($"Поиск препарата в списке компании: '{drugName}' с порогом {threshold}");
            
            var companyDrugs = await GetCompanyDrugsAsync();
            var normalizedSearchName = NormalizeDrugName(drugName);

            CompanyDrug? bestMatch = null;
            int bestScore = 0;

            foreach (var drug in companyDrugs.Where(d => d.IsActive))
            {
                var normalizedDrugName = NormalizeDrugName(drug.Name);
                
                // Проверяем точное совпадение
                if (normalizedDrugName == normalizedSearchName)
                {
                    _logger.Info($"Найдено точное совпадение в списке компании: '{drug.Name}'");
                    return drug;
                }

                // Проверяем содержание подстроки
                if (normalizedDrugName.Contains(normalizedSearchName) || normalizedSearchName.Contains(normalizedDrugName))
                {
                    _logger.Info($"Найдено совпадение по подстроке в списке компании: '{drug.Name}'");
                    return drug;
                }

                // Используем нечёткий поиск
                var score = Fuzz.Ratio(normalizedSearchName, normalizedDrugName);
                var partialScore = Fuzz.PartialRatio(normalizedSearchName, normalizedDrugName);
                var tokenScore = Fuzz.TokenSortRatio(normalizedSearchName, normalizedDrugName);
                
                var maxScore = Math.Max(score, Math.Max(partialScore, tokenScore));

                if (maxScore > bestScore && maxScore >= threshold)
                {
                    bestScore = maxScore;
                    bestMatch = drug;
                }
            }

            if (bestMatch != null)
            {
                _logger.Info($"Найден препарат в списке компании: '{bestMatch.Name}' (схожесть: {bestScore}%)");
            }
            else
            {
                _logger.Warn($"Препарат '{drugName}' не найден в списке компании с порогом {threshold}%");
            }

            return bestMatch;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при поиске препарата '{drugName}' в списке компании");
            return null;
        }
    }

    /// <summary>
    /// Поиск нескольких препаратов в прайс-листе по названиям
    /// </summary>
    /// <param name="drugNames">Список названий препаратов для поиска</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 60)</param>
    /// <returns>Список найденных препаратов из прайс-листа</returns>
    public async Task<List<PriceListItem>> FindMultipleDrugsInPriceListAsync(IEnumerable<string> drugNames, int threshold = 60)
    {
        var results = new List<PriceListItem>();

        foreach (var drugName in drugNames.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var found = await FindDrugInPriceListAsync(drugName, threshold);
            if (found != null)
            {
                results.Add(found);
            }
        }

        _logger.Info($"Найдено препаратов в прайс-листе: {results.Count} из {drugNames.Count()}");
        return results;
    }

    /// <summary>
    /// Поиск нескольких препаратов в списке препаратов компании по названиям
    /// </summary>
    /// <param name="drugNames">Список названий препаратов для поиска</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 60)</param>
    /// <returns>Список найденных препаратов компании</returns>
    public async Task<List<CompanyDrug>> FindMultipleCompanyDrugsAsync(IEnumerable<string> drugNames, int threshold = 60)
    {
        var results = new List<CompanyDrug>();

        foreach (var drugName in drugNames.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var found = await FindCompanyDrugAsync(drugName, threshold);
            if (found != null)
            {
                results.Add(found);
            }
        }

        _logger.Info($"Найдено препаратов в списке компании: {results.Count} из {drugNames.Count()}");
        return results;
    }

    /// <summary>
    /// Получение всех препаратов из прайс-листа по частичному названию
    /// </summary>
    /// <param name="partialName">Частичное название препарата</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 50)</param>
    /// <param name="maxResults">Максимальное количество результатов (по умолчанию 10)</param>
    /// <returns>Список наиболее похожих препаратов</returns>
    public async Task<List<PriceListItem>> SearchSimilarDrugsAsync(string partialName, int threshold = 50, int maxResults = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(partialName))
            {
                _logger.Warn("Пустое частичное название для поиска похожих препаратов");
                return new List<PriceListItem>();
            }

            _logger.Info($"Поиск похожих препаратов по названию: '{partialName}' с порогом {threshold}%, максимум результатов: {maxResults}");
            
            var priceList = await GetPriceListAsync();
            var normalizedSearchName = NormalizeDrugName(partialName);

            var scoredResults = new List<(PriceListItem item, int score)>();

            foreach (var item in priceList)
            {
                if (item.Drug?.DrugName == null) continue;

                var normalizedDrugName = NormalizeDrugName(item.Drug.DrugName);
                
                var score = Fuzz.Ratio(normalizedSearchName, normalizedDrugName);
                var partialScore = Fuzz.PartialRatio(normalizedSearchName, normalizedDrugName);
                var tokenScore = Fuzz.TokenSortRatio(normalizedSearchName, normalizedDrugName);
                
                var maxScore = Math.Max(score, Math.Max(partialScore, tokenScore));

                if (maxScore >= threshold)
                {
                    scoredResults.Add((item, maxScore));
                }
            }

            var results = scoredResults
                .OrderByDescending(x => x.score)
                .Take(maxResults)
                .Select(x => x.item)
                .ToList();

            _logger.Info($"Найдено похожих препаратов: {results.Count}");
            return results;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при поиске похожих препаратов по названию '{partialName}'");
            return new List<PriceListItem>();
        }
    }
}