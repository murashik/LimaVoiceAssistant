using LimaVoiceAssistant.Models;

namespace LimaVoiceAssistant.Services;

/// <summary>
/// Интерфейс сервиса для выполнения основных функций Lima
/// </summary>
public interface ILimaFunctionsService
{
    /// <summary>
    /// Функция №1: Создание брони в аптеку
    /// </summary>
    /// <param name="pharmacyName">Название аптеки</param>
    /// <param name="drugs">Список препаратов с количеством</param>
    /// <param name="prepaymentPercent">Процент предоплаты (по умолчанию 100)</param>
    /// <param name="paymentType">Тип оплаты: "наличные" или "перечисление"</param>
    /// <param name="comment">Комментарий к заказу</param>
    /// <returns>Результат создания брони</returns>
    Task<string> CreatePharmacyReservationAsync(string pharmacyName, List<DrugOrderItem> drugs, 
        decimal prepaymentPercent = 100, string paymentType = "наличные", string? comment = null);

    /// <summary>
    /// Функция №2: Создание визита в ЛПУ
    /// </summary>
    /// <param name="clinicName">Название клиники/ЛПУ</param>
    /// <param name="doctorName">ФИО врача</param>
    /// <param name="discussedDrugs">Список препаратов, о которых говорили</param>
    /// <param name="latitude">Широта (опционально)</param>
    /// <param name="longitude">Долгота (опционально)</param>
    /// <param name="comment">Комментарий к визиту</param>
    /// <returns>Результат создания визита</returns>
    Task<string> CreateClinicVisitAsync(string clinicName, string? doctorName, List<string> discussedDrugs, 
        double? latitude = null, double? longitude = null, string? comment = null);

    /// <summary>
    /// Функция №3: Получение истории визитов и заказов
    /// </summary>
    /// <param name="visitType">Тип визита: "аптека", "лпу" или пустое для всех</param>
    /// <param name="organizationName">Название организации для фильтрации</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="date">Дата для фильтрации: "сегодня", "вчера", "пятница" или "YYYY-MM-DD"</param>
    /// <returns>История визитов в текстовом формате</returns>
    Task<string> GetVisitHistoryAsync(string? visitType = null, string? organizationName = null, int page = 1, string? date = null);

    /// <summary>
    /// Функция №4: Поиск организации по названию
    /// </summary>
    /// <param name="organizationName">Название организации</param>
    /// <returns>Информация о найденных организациях</returns>
    Task<string> SearchOrganizationsAsync(string organizationName);

    /// <summary>
    /// Функция №5: Просмотр плана визитов
    /// </summary>
    /// <param name="date">Дата в формате "YYYY-MM-DD" или названия дней недели</param>
    /// <param name="viewType">Тип просмотра: "день", "месяц" или "неделя"</param>
    /// <returns>План визитов в текстовом формате</returns>
    Task<string> GetPlannedVisitsAsync(string? date = null, string viewType = "день");

    /// <summary>
    /// Получение остатков препарата по названию
    /// </summary>
    /// <param name="drugName">Название препарата</param>
    /// <returns>Информация об остатках</returns>
    Task<string> GetDrugStockAsync(string drugName);
}