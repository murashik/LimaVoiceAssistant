using Newtonsoft.Json;

namespace LimaVoiceAssistant.Models;

/// <summary>
/// Модель препарата
/// </summary>
public class Drug
{
    /// <summary>
    /// Идентификатор препарата
    /// </summary>
    [JsonProperty("drug_id")]
    public int DrugId { get; set; }

    /// <summary>
    /// Название препарата
    /// </summary>
    [JsonProperty("drug_name")]
    public string DrugName { get; set; } = string.Empty;

    /// <summary>
    /// Количество в упаковке
    /// </summary>
    [JsonProperty("quantity")]
    public int? Quantity { get; set; }
}

/// <summary>
/// Модель позиции прайс-листа
/// </summary>
public class PriceListItem
{
    /// <summary>
    /// Идентификатор детализации прихода
    /// </summary>
    [JsonProperty("income_detailing_id")]
    public int IncomeDetailingId { get; set; }

    /// <summary>
    /// Информация о препарате
    /// </summary>
    [JsonProperty("drug")]
    public Drug Drug { get; set; } = new();

    /// <summary>
    /// Актуальный остаток
    /// </summary>
    [JsonProperty("actual_balance")]
    public int ActualBalance { get; set; }

    /// <summary>
    /// Цена
    /// </summary>
    [JsonProperty("price")]
    public decimal? Price { get; set; }
}

/// <summary>
/// Модель препарата для заказа
/// </summary>
public class OrderDrug
{
    /// <summary>
    /// Идентификатор детализации прихода
    /// </summary>
    [JsonProperty("income_detailing_id")]
    public int IncomeDetailingId { get; set; }

    /// <summary>
    /// Идентификатор препарата
    /// </summary>
    [JsonProperty("drug_id")]
    public int DrugId { get; set; }

    /// <summary>
    /// Количество упаковок
    /// </summary>
    [JsonProperty("package")]
    public int Package { get; set; }
}

/// <summary>
/// Модель препарата для визита в ЛПУ
/// </summary>
public class TalkedAboutDrug
{
    /// <summary>
    /// Идентификатор препарата
    /// </summary>
    [JsonProperty("drug_id")]
    public int DrugId { get; set; }

    /// <summary>
    /// Идентификатор статуса (может быть null)
    /// </summary>
    [JsonProperty("status_id")]
    public int? StatusId { get; set; }

    /// <summary>
    /// Название препарата (возвращается в истории)
    /// </summary>
    [JsonProperty("drug_name")]
    public string? DrugName { get; set; }

    /// <summary>
    /// Название статуса (возвращается в истории)
    /// </summary>
    [JsonProperty("status_name")]
    public string? StatusName { get; set; }

    /// <summary>
    /// Подтверждён ли препарат (возвращается в истории)
    /// </summary>
    [JsonProperty("is_confirmed")]
    public bool? IsConfirmed { get; set; }
}

/// <summary>
/// Модель препарата компании
/// </summary>
public class CompanyDrug
{
    /// <summary>
    /// Идентификатор препарата
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// Название препарата
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Описание препарата
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Активное ли вещество
    /// </summary>
    [JsonProperty("is_active")]
    public bool IsActive { get; set; }
}