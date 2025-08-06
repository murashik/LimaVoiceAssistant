using LimaVoiceAssistant.Configuration;
using LimaVoiceAssistant.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;
using System.Text;

namespace LimaVoiceAssistant.Clients;

/// <summary>
/// Клиент для работы с Lima API
/// </summary>
public class LimaApiClient : ILimaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly LimaApiSettings _settings;
    private readonly NLog.ILogger _logger;

    public LimaApiClient(HttpClient httpClient, IOptions<LimaApiSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = LogManager.GetCurrentClassLogger();

        // Настройка базового URL и авторизации
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.JwtToken}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Поиск организаций по названию
    /// </summary>
    /// <param name="searchQuery">Поисковый запрос</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список найденных организаций</returns>
    public async Task<OrganizationSearchResponse> SearchOrganizationsAsync(string searchQuery, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Поиск организаций по запросу: {searchQuery}");
            
            var encodedQuery = Uri.EscapeDataString(searchQuery);
            var response = await _httpClient.GetAsync($"/dict/organizations/find?q={encodedQuery}", cancellationToken);
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var result = JsonConvert.DeserializeObject<OrganizationSearchResponse>(json);
            _logger.Info($"Найдено организаций: {result?.Result?.Count ?? 0}");
            
            return result ?? new OrganizationSearchResponse();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при поиске организаций: {searchQuery}");
            throw;
        }
    }

    /// <summary>
    /// Получение списка вариантов предоплаты
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список вариантов предоплаты</returns>
    public async Task<List<Margin>> GetMarginsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info("Получение списка вариантов предоплаты");
            
            var response = await _httpClient.GetAsync("/company/markups/short", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<List<Margin>>(json);
            
            _logger.Info($"Получено вариантов предоплаты: {result?.Count ?? 0}");
            return result ?? new List<Margin>();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при получении вариантов предоплаты");
            throw;
        }
    }

    /// <summary>
    /// Получение прайс-листа препаратов
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список препаратов с остатками</returns>
    public async Task<List<PriceListItem>> GetPriceListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info("Получение прайс-листа препаратов");
            
            var response = await _httpClient.GetAsync("/stock/price-list", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<List<PriceListItem>>(json);
            
            _logger.Info($"Получено препаратов в прайс-листе: {result?.Count ?? 0}");
            return result ?? new List<PriceListItem>();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при получении прайс-листа");
            throw;
        }
    }

    /// <summary>
    /// Создание визита или брони
    /// </summary>
    /// <param name="request">Запрос на создание визита</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат создания визита</returns>
    public async Task<bool> CreateVisitAsync(CreateVisitRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Создание визита типа {request.VisitType} в организацию {request.OrganizationId}");
            
            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Определяем endpoint в зависимости от типа визита
            var response = await _httpClient.PostAsync("/visits/add", content, cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            _logger.Info($"Визит успешно создан в организацию {request.OrganizationId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при создании визита в организацию {request.OrganizationId}");
            throw;
        }
    }

    /// <summary>
    /// Получение списка врачей организации
    /// </summary>
    /// <param name="organizationId">Идентификатор организации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список врачей</returns>
    public async Task<List<Doctor>> GetOrganizationDoctorsAsync(int organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Получение списка врачей для организации {organizationId}");
            
            var response = await _httpClient.GetAsync($"/dict/organizations/{organizationId}/doctors", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<List<Doctor>>(json);
            
            _logger.Info($"Получено врачей: {result?.Count ?? 0}");
            return result ?? new List<Doctor>();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при получении врачей организации {organizationId}");
            throw;
        }
    }

    /// <summary>
    /// Получение списка препаратов компании
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список препаратов компании</returns>
    public async Task<List<CompanyDrug>> GetCompanyDrugsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info("Получение списка препаратов компании");
            
            var response = await _httpClient.GetAsync("/company/drugs", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<List<CompanyDrug>>(json);
            
            _logger.Info($"Получено препаратов компании: {result?.Count ?? 0}");
            return result ?? new List<CompanyDrug>();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при получении препаратов компании");
            throw;
        }
    }

    /// <summary>
    /// Получение истории визитов
    /// </summary>
    /// <param name="page">Номер страницы</param>
    /// <param name="typeId">Тип визита (1 - аптека, 2 - ЛПУ)</param>
    /// <param name="searchQuery">Поисковый запрос по организации</param>
    /// <param name="startDate">Дата начала периода в формате YYYY-MM-DD</param>
    /// <param name="endDate">Дата окончания периода в формате YYYY-MM-DD</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>История визитов</returns>
    public async Task<VisitHistoryResponse> GetVisitHistoryAsync(int page = 1, int? typeId = null, string? searchQuery = null, string? startDate = null, string? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Получение истории визитов: страница {page}, тип {typeId}, запрос: {searchQuery}, период: {startDate} - {endDate}");
            
            var queryParams = new List<string> { $"page={page}" };
            
            if (typeId.HasValue)
                queryParams.Add($"type_id={typeId.Value}");
                
            if (!string.IsNullOrEmpty(searchQuery))
                queryParams.Add($"q={Uri.EscapeDataString(searchQuery)}");

            // Добавляем параметры dates для диапазона дат
            if (!string.IsNullOrEmpty(startDate))
            {
                queryParams.Add($"dates={startDate}");
                
                // Если endDate не указана, используем startDate как конечную дату (один день)
                var effectiveEndDate = !string.IsNullOrEmpty(endDate) ? endDate : startDate;
                queryParams.Add($"dates={effectiveEndDate}");
            }
            
            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/visits/history?{queryString}", cancellationToken);
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var result = JsonConvert.DeserializeObject<VisitHistoryResponse>(json);
            _logger.Info($"Получено визитов: {result?.Result?.Count ?? 0}");
            
            return result ?? new VisitHistoryResponse();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при получении истории визитов");
            throw;
        }
    }

    /// <summary>
    /// Получение количества визитов по датам месяца
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество визитов по датам</returns>
    public async Task<List<VisitCountByDate>> GetMonthPlansAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info("Получение количества визитов по датам месяца");
            
            var response = await _httpClient.GetAsync("/plans/month", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<List<VisitCountByDate>>(json);
            
            _logger.Info($"Получено дат с визитами: {result?.Count ?? 0}");
            return result ?? new List<VisitCountByDate>();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при получении плана визитов на месяц");
            throw;
        }
    }

    /// <summary>
    /// Получение запланированных визитов на дату
    /// </summary>
    /// <param name="date">Дата в формате YYYY-MM-DD</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список запланированных визитов</returns>
    public async Task<PlannedVisitsResponse> GetPlannedVisitsAsync(string date, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Получение запланированных визитов на дату: {date}");
            
            var response = await _httpClient.GetAsync($"/plans/current?date={date}", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<PlannedVisitsResponse>(json);
            
            _logger.Info($"Получено запланированных визитов: {result?.Result?.Count ?? 0}");
            return result ?? new PlannedVisitsResponse();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при получении запланированных визитов на дату {date}");
            throw;
        }
    }
}