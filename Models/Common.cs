using Newtonsoft.Json;

namespace LimaVoiceAssistant.Models;

/// <summary>
/// Информация о пагинации
/// </summary>
public class PageInfo
{
    /// <summary>
    /// Номер текущей страницы
    /// </summary>
    [JsonProperty("page_number")]
    public int PageNumber { get; set; }

    /// <summary>
    /// Общее количество страниц
    /// </summary>
    [JsonProperty("total_pages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    [JsonProperty("page_size")]
    public int PageSize { get; set; }

    /// <summary>
    /// Общее количество элементов
    /// </summary>
    [JsonProperty("count")]
    public int Count { get; set; }

    /// <summary>
    /// Общее количество элементов (альтернативное название)
    /// </summary>
    [JsonProperty("total_items")]
    public int? TotalItems { get; set; }

    /// <summary>
    /// Есть ли предыдущая страница
    /// </summary>
    [JsonProperty("has_previous_page")]
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Есть ли следующая страница
    /// </summary>
    [JsonProperty("has_next_page")]
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Модель врача
/// </summary>
public class Doctor
{
    /// <summary>
    /// Идентификатор врача
    /// </summary>
    [JsonProperty("doctor_id")]
    public int DoctorId { get; set; }

    /// <summary>
    /// ФИО врача
    /// </summary>
    [JsonProperty("doctor_name")]
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>
    /// Должность врача
    /// </summary>
    [JsonProperty("doctor_position")]
    public string? DoctorPosition { get; set; }

    /// <summary>
    /// Телефон врача
    /// </summary>
    [JsonProperty("doctor_phone")]
    public string? DoctorPhone { get; set; }
}

/// <summary>
/// Модель медпредставителя
/// </summary>
public class MedRep
{
    /// <summary>
    /// Идентификатор медпредставителя
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// ФИО медпредставителя
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Регион медпредставителя
    /// </summary>
    [JsonProperty("region_name")]
    public string? RegionName { get; set; }

    /// <summary>
    /// Телефон медпредставителя
    /// </summary>
    [JsonProperty("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Информация о компании
    /// </summary>
    [JsonProperty("company")]
    public Company? Company { get; set; }
}

/// <summary>
/// Модель компании
/// </summary>
public class Company
{
    /// <summary>
    /// Идентификатор компании
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// Название компании
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Модель ответа API
/// </summary>
/// <typeparam name="T">Тип данных в результате</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Успешность запроса
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Сообщение об ошибке
    /// </summary>
    [JsonProperty("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Результат запроса
    /// </summary>
    [JsonProperty("result")]
    public T? Result { get; set; }

    /// <summary>
    /// Информация о пагинации (если применимо)
    /// </summary>
    [JsonProperty("page")]
    public PageInfo? Page { get; set; }
}

/// <summary>
/// Модель запроса к голосовому помощнику
/// </summary>
public class AssistantRequest
{
    /// <summary>
    /// Текстовое сообщение пользователя
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор сессии пользователя для сохранения контекста
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// Модель ответа голосового помощника
/// </summary>
public class AssistantResponse
{
    /// <summary>
    /// Текстовый ответ пользователю
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Успешность обработки запроса
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Сообщение об ошибке (если есть)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Название функции, которая была выполнена
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Контекст был очищен
    /// </summary>
    public bool ContextCleared { get; set; } = false;
}