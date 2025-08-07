using LimaVoiceAssistant.Models;

namespace LimaVoiceAssistant.Services;

/// <summary>
/// Интерфейс сервиса для поиска препаратов с использованием нечёткого поиска
/// </summary>
public interface IDrugSearchService
{
    /// <summary>
    /// Поиск препарата в прайс-листе по названию с использованием нечёткого поиска
    /// </summary>
    /// <param name="drugName">Название препарата для поиска</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 60)</param>
    /// <returns>Найденный препарат из прайс-листа или null</returns>
    Task<PriceListItem?> FindDrugInPriceListAsync(string drugName, int threshold = 60);

    /// <summary>
    /// Поиск препарата в списке препаратов компании по названию с использованием нечёткого поиска
    /// </summary>
    /// <param name="drugName">Название препарата для поиска</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 60)</param>
    /// <returns>Найденный препарат компании или null</returns>
    Task<CompanyDrug?> FindCompanyDrugAsync(string drugName, int threshold = 60);

    /// <summary>
    /// Поиск нескольких препаратов в прайс-листе по названиям
    /// </summary>
    /// <param name="drugNames">Список названий препаратов для поиска</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 60)</param>
    /// <returns>Список найденных препаратов из прайс-листа</returns>
    Task<List<PriceListItem>> FindMultipleDrugsInPriceListAsync(IEnumerable<string> drugNames, int threshold = 60);

    /// <summary>
    /// Поиск нескольких препаратов в списке препаратов компании по названиям
    /// </summary>
    /// <param name="drugNames">Список названий препаратов для поиска</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 60)</param>
    /// <returns>Список найденных препаратов компании</returns>
    Task<List<CompanyDrug>> FindMultipleCompanyDrugsAsync(IEnumerable<string> drugNames, int threshold = 60);

    /// <summary>
    /// Получение всех препаратов из прайс-листа по частичному названию
    /// </summary>
    /// <param name="partialName">Частичное название препарата</param>
    /// <param name="threshold">Минимальный порог схожести (по умолчанию 50)</param>
    /// <param name="maxResults">Максимальное количество результатов (по умолчанию 10)</param>
    /// <returns>Список наиболее похожих препаратов</returns>
    Task<List<PriceListItem>> SearchSimilarDrugsAsync(string partialName, int threshold = 50, int maxResults = 10);

    /// <summary>
    /// Получение остатков всех препаратов из прайс-листа
    /// </summary>
    /// <returns>Список всех препаратов с остатками</returns>
    Task<List<PriceListItem>> GetAllDrugBalancesAsync();
}