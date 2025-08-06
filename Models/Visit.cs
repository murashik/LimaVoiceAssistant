using Newtonsoft.Json;

namespace LimaVoiceAssistant.Models;

/// <summary>
/// Модель создания визита/брони
/// </summary>
public class CreateVisitRequest
{
    /// <summary>
    /// Идентификатор организации
    /// </summary>
    [JsonProperty("organization_id")]
    public int OrganizationId { get; set; }

    /// <summary>
    /// Тип визита (1 - аптека, 2 - ЛПУ)
    /// </summary>
    [JsonProperty("visit_type")]
    public int VisitType { get; set; }

    /// <summary>
    /// Идентификатор маржи (только для брони в аптеку)
    /// </summary>
    [JsonProperty("margin_id")]
    public int? MarginId { get; set; }

    /// <summary>
    /// Является ли оптовиком
    /// </summary>
    [JsonProperty("is_wholesaler")]
    public bool? IsWholesaler { get; set; }

    /// <summary>
    /// Завершён ли визит
    /// </summary>
    [JsonProperty("complete")]
    public bool Complete { get; set; }

    /// <summary>
    /// Идентификатор варианта оплаты (1 - перечисление, 2 - наличные)
    /// </summary>
    [JsonProperty("payment_variant_id")]
    public int? PaymentVariantId { get; set; }

    /// <summary>
    /// Комментарий к визиту
    /// </summary>
    [JsonProperty("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// Широта (для визитов в ЛПУ)
    /// </summary>
    [JsonProperty("latitude")]
    public double? Latitude { get; set; }

    /// <summary>
    /// Долгота (для визитов в ЛПУ)
    /// </summary>
    [JsonProperty("longitude")]
    public double? Longitude { get; set; }

    /// <summary>
    /// Идентификатор врача (для визитов в ЛПУ)
    /// </summary>
    [JsonProperty("doctor_id")]
    public int? DoctorId { get; set; }

    /// <summary>
    /// Список препаратов для заказа (для брони в аптеку)
    /// </summary>
    [JsonProperty("drugs")]
    public List<OrderDrug>? Drugs { get; set; }

    /// <summary>
    /// Список препаратов, о которых говорили (для визитов в ЛПУ)
    /// </summary>
    [JsonProperty("talked_about_drugs")]
    public List<TalkedAboutDrug>? TalkedAboutDrugs { get; set; }
}

/// <summary>
/// Модель визита из истории
/// </summary>
public class VisitHistoryItem
{
    /// <summary>
    /// Идентификатор визита
    /// </summary>
    [JsonProperty("visit_id")]
    public int VisitId { get; set; }

    /// <summary>
    /// Тип визита (1 - аптека, 2 - ЛПУ)
    /// </summary>
    [JsonProperty("visit_type")]
    public int VisitType { get; set; }

    /// <summary>
    /// Статус визита
    /// </summary>
    [JsonProperty("visit_status")]
    public int VisitStatus { get; set; }

    /// <summary>
    /// Название статуса визита
    /// </summary>
    [JsonProperty("visit_status_name")]
    public string VisitStatusName { get; set; } = string.Empty;

    /// <summary>
    /// Статус заказа
    /// </summary>
    [JsonProperty("order_status")]
    public int OrderStatus { get; set; }

    /// <summary>
    /// Название статуса заказа
    /// </summary>
    [JsonProperty("order_status_name")]
    public string OrderStatusName { get; set; } = string.Empty;

    /// <summary>
    /// Дата создания визита
    /// </summary>
    [JsonProperty("date_create")]
    public DateTime DateCreate { get; set; }

    /// <summary>
    /// Общая сумма заказа
    /// </summary>
    [JsonProperty("total_sum")]
    public decimal TotalSum { get; set; }

    /// <summary>
    /// Процент маржи
    /// </summary>
    [JsonProperty("margin_percent")]
    public decimal MarginPercent { get; set; }

    /// <summary>
    /// Информация о враче
    /// </summary>
    [JsonProperty("doctor")]
    public Doctor? Doctor { get; set; }

    /// <summary>
    /// Препараты, о которых говорили (для визитов в ЛПУ)
    /// </summary>
    [JsonProperty("talked_about_drugs")]
    public List<TalkedAboutDrug> TalkedAboutDrugs { get; set; } = new();

    /// <summary>
    /// Заказанные препараты (для брони в аптеку)
    /// </summary>
    [JsonProperty("drugs")]
    public List<OrderDrug> Drugs { get; set; } = new();

    /// <summary>
    /// Информация об организации
    /// </summary>
    [JsonProperty("organization")]
    public Organization Organization { get; set; } = new();

    /// <summary>
    /// Информация о медпредставителе
    /// </summary>
    [JsonProperty("medrep")]
    public MedRep MedRep { get; set; } = new();
}

/// <summary>
/// Ответ API истории визитов
/// </summary>
public class VisitHistoryResponse
{
    /// <summary>
    /// Информация о пагинации
    /// </summary>
    [JsonProperty("page")]
    public PageInfo Page { get; set; } = new();

    /// <summary>
    /// Список визитов
    /// </summary>
    [JsonProperty("result")]
    public List<VisitHistoryItem> Result { get; set; } = new();
}

/// <summary>
/// Модель запланированного визита
/// </summary>
public class PlannedVisit
{
    /// <summary>
    /// Идентификатор визита
    /// </summary>
    [JsonProperty("visit_id")]
    public int VisitId { get; set; }

    /// <summary>
    /// Статус визита
    /// </summary>
    [JsonProperty("visit_status")]
    public int VisitStatus { get; set; }

    /// <summary>
    /// Название статуса визита
    /// </summary>
    [JsonProperty("visit_status_name")]
    public string VisitStatusName { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала визита
    /// </summary>
    [JsonProperty("start_date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Информация об организации
    /// </summary>
    [JsonProperty("organization")]
    public Organization Organization { get; set; } = new();

    /// <summary>
    /// Информация о враче
    /// </summary>
    [JsonProperty("doctor")]
    public Doctor? Doctor { get; set; }

    /// <summary>
    /// Информация о медпредставителе
    /// </summary>
    [JsonProperty("medrep")]
    public MedRep MedRep { get; set; } = new();
}

/// <summary>
/// Ответ API запланированных визитов
/// </summary>
public class PlannedVisitsResponse
{
    /// <summary>
    /// Информация о пагинации
    /// </summary>
    [JsonProperty("page")]
    public PageInfo Page { get; set; } = new();

    /// <summary>
    /// Список запланированных визитов
    /// </summary>
    [JsonProperty("result")]
    public List<PlannedVisit> Result { get; set; } = new();
}

/// <summary>
/// Модель количества визитов по дате
/// </summary>
public class VisitCountByDate
{
    /// <summary>
    /// Дата
    /// </summary>
    [JsonProperty("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Количество визитов
    /// </summary>
    [JsonProperty("visit_count")]
    public int VisitCount { get; set; }
}