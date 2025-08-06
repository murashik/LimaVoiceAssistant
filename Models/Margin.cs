using Newtonsoft.Json;

namespace LimaVoiceAssistant.Models;

/// <summary>
/// Модель варианта предоплаты (маржи)
/// </summary>
public class Margin
{
    /// <summary>
    /// Идентификатор маржи
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// Процент предоплаты
    /// </summary>
    [JsonProperty("prepayment_percent")]
    public decimal PrepaymentPercent { get; set; }

    /// <summary>
    /// Доступно ли для оптовиков
    /// </summary>
    [JsonProperty("wholesaler")]
    public bool Wholesaler { get; set; }

    /// <summary>
    /// Доступно ли для розницы
    /// </summary>
    [JsonProperty("retail")]
    public bool Retail { get; set; }

    /// <summary>
    /// Название маржи
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Описание маржи
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }
}