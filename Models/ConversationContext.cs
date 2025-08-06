using Newtonsoft.Json;

namespace LimaVoiceAssistant.Models;

/// <summary>
/// Контекст незавершённой операции
/// </summary>
public class PendingOperation
{
    /// <summary>
    /// Тип операции (createPharmacyReservation, createClinicVisit и т.д.)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Частично заполненные параметры операции
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Время создания операции
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Список недостающих параметров
    /// </summary>
    public List<string> MissingParameters { get; set; } = new();

    /// <summary>
    /// Описание того, что ещё нужно уточнить у пользователя
    /// </summary>
    public string? NextQuestion { get; set; }
}

/// <summary>
/// Контекст диалога с пользователем
/// </summary>
public class ConversationContext
{
    /// <summary>
    /// Уникальный идентификатор сессии
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// История сообщений в диалоге
    /// </summary>
    public List<ConversationMessage> Messages { get; set; } = new();

    /// <summary>
    /// Незавершённая операция (если есть)
    /// </summary>
    public PendingOperation? PendingOperation { get; set; }

    /// <summary>
    /// Время последнего обновления контекста
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Время создания контекста
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дополнительные данные контекста
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Сообщение в диалоге
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Роль отправителя (user, assistant, system)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Содержание сообщения
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Время отправки сообщения
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Название функции (если сообщение содержит вызов функции)
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Аргументы функции в JSON формате
    /// </summary>
    public string? FunctionArguments { get; set; }
}

/// <summary>
/// Параметры для создания брони в аптеку
/// </summary>
public class PharmacyReservationContext
{
    /// <summary>
    /// Название аптеки
    /// </summary>
    public string? PharmacyName { get; set; }

    /// <summary>
    /// ID найденной аптеки
    /// </summary>
    public int? PharmacyId { get; set; }

    /// <summary>
    /// Список препаратов с количеством
    /// </summary>
    public List<DrugOrderItem> Drugs { get; set; } = new();

    /// <summary>
    /// Процент предоплаты
    /// </summary>
    public decimal? PrepaymentPercent { get; set; }

    /// <summary>
    /// ID маржи
    /// </summary>
    public int? MarginId { get; set; }

    /// <summary>
    /// Тип оплаты (1 - перечисление, 2 - наличные)
    /// </summary>
    public int? PaymentVariantId { get; set; }

    /// <summary>
    /// Комментарий к заказу
    /// </summary>
    public string? Comment { get; set; }
}

/// <summary>
/// Параметры для создания визита в ЛПУ
/// </summary>
public class ClinicVisitContext
{
    /// <summary>
    /// Название клиники/ЛПУ
    /// </summary>
    public string? ClinicName { get; set; }

    /// <summary>
    /// ID найденной клиники
    /// </summary>
    public int? ClinicId { get; set; }

    /// <summary>
    /// Имя врача
    /// </summary>
    public string? DoctorName { get; set; }

    /// <summary>
    /// ID врача
    /// </summary>
    public int? DoctorId { get; set; }

    /// <summary>
    /// Список препаратов, о которых говорили
    /// </summary>
    public List<string> DiscussedDrugs { get; set; } = new();

    /// <summary>
    /// Широта
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Долгота
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Комментарий к визиту
    /// </summary>
    public string? Comment { get; set; }
}

/// <summary>
/// Элемент заказа препарата
/// </summary>
public class DrugOrderItem
{
    /// <summary>
    /// Название препарата
    /// </summary>
    public string DrugName { get; set; } = string.Empty;

    /// <summary>
    /// ID препарата
    /// </summary>
    public int? DrugId { get; set; }

    /// <summary>
    /// ID детализации прихода
    /// </summary>
    public int? IncomeDetailingId { get; set; }

    /// <summary>
    /// Количество упаковок
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Найден ли препарат в системе
    /// </summary>
    public bool IsFound { get; set; }
}