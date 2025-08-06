using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace LimaVoiceAssistant.Models;

/// <summary>
/// Модель организации (аптеки или ЛПУ) из API Lima
/// </summary>
public class Organization
{
    /// <summary>
    /// Уникальный идентификатор организации
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// Наименование организации
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Адрес организации
    /// </summary>
    [JsonProperty("address")]
    public string? Address { get; set; }

    /// <summary>
    /// Адрес доставки
    /// </summary>
    [JsonProperty("delivery_address")]
    public string? DeliveryAddress { get; set; }

    /// <summary>
    /// Идентификатор типа организации
    /// </summary>
    [JsonProperty("type_id")]
    public int TypeId { get; set; }

    /// <summary>
    /// Название типа организации (Аптека, ЛПУ и т.д.)
    /// </summary>
    [JsonProperty("type_name")]
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор региона
    /// </summary>
    [JsonProperty("region_id")]
    public int RegionId { get; set; }

    /// <summary>
    /// Название региона
    /// </summary>
    [JsonProperty("region_name")]
    public string RegionName { get; set; } = string.Empty;

    /// <summary>
    /// Название района
    /// </summary>
    [JsonProperty("area_name")]
    public string? AreaName { get; set; }

    /// <summary>
    /// ИНН организации
    /// </summary>
    [JsonProperty("inn")]
    public long? Inn { get; set; }

    /// <summary>
    /// Телефон организации
    /// </summary>
    [JsonProperty("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Идентификатор медпредставителя
    /// </summary>
    [JsonProperty("med_rep_id")]
    public int? MedRepId { get; set; }
}

/// <summary>
/// Ответ API поиска организаций
/// </summary>
public class OrganizationSearchResponse
{
    /// <summary>
    /// Информация о пагинации
    /// </summary>
    [JsonProperty("page")]
    public PageInfo Page { get; set; } = new();

    /// <summary>
    /// Список найденных организаций
    /// </summary>
    [JsonProperty("result")]
    public List<Organization> Result { get; set; } = new();
}