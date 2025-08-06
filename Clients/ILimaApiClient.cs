using LimaVoiceAssistant.Models;

namespace LimaVoiceAssistant.Clients;

/// <summary>
/// Интерфейс клиента для работы с Lima API
/// </summary>
public interface ILimaApiClient
{
    /// <summary>
    /// Поиск организаций по названию
    /// </summary>
    /// <param name="searchQuery">Поисковый запрос</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список найденных организаций</returns>
    Task<OrganizationSearchResponse> SearchOrganizationsAsync(string searchQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение списка вариантов предоплаты
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список вариантов предоплаты</returns>
    Task<List<Margin>> GetMarginsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение прайс-листа препаратов
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список препаратов с остатками</returns>
    Task<List<PriceListItem>> GetPriceListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создание визита или брони
    /// </summary>
    /// <param name="request">Запрос на создание визита</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат создания визита</returns>
    Task<bool> CreateVisitAsync(CreateVisitRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение списка врачей организации
    /// </summary>
    /// <param name="organizationId">Идентификатор организации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список врачей</returns>
    Task<List<Doctor>> GetOrganizationDoctorsAsync(int organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение списка препаратов компании
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список препаратов компании</returns>
    Task<List<CompanyDrug>> GetCompanyDrugsAsync(CancellationToken cancellationToken = default);

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
    Task<VisitHistoryResponse> GetVisitHistoryAsync(int page = 1, int? typeId = null, string? searchQuery = null, string? startDate = null, string? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение количества визитов по датам месяца
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество визитов по датам</returns>
    Task<List<VisitCountByDate>> GetMonthPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение запланированных визитов на дату
    /// </summary>
    /// <param name="date">Дата в формате YYYY-MM-DD</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список запланированных визитов</returns>
    Task<PlannedVisitsResponse> GetPlannedVisitsAsync(string date, CancellationToken cancellationToken = default);
}