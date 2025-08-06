using Newtonsoft.Json;

namespace LimaVoiceAssistant.Models;

/// <summary>
/// Модель сообщения для OpenAI API
/// </summary>
public class OpenAIMessage
{
    /// <summary>
    /// Роль отправителя (system, user, assistant, function)
    /// </summary>
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Содержание сообщения
    /// </summary>
    [JsonProperty("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Название функции (для role = function)
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Вызов функции
    /// </summary>
    [JsonProperty("function_call")]
    public FunctionCall? FunctionCall { get; set; }
}

/// <summary>
/// Модель вызова функции
/// </summary>
public class FunctionCall
{
    /// <summary>
    /// Название функции
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Аргументы функции в JSON формате
    /// </summary>
    [JsonProperty("arguments")]
    public string Arguments { get; set; } = "{}";
}

/// <summary>
/// Модель параметра функции
/// </summary>
public class FunctionParameter
{
    /// <summary>
    /// Тип параметра
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Описание параметра
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Возможные значения (для enum)
    /// </summary>
    [JsonProperty("enum")]
    public List<string>? Enum { get; set; }

    /// <summary>
    /// Свойства объекта (для type = object)
    /// </summary>
    [JsonProperty("properties")]
    public Dictionary<string, FunctionParameter>? Properties { get; set; }

    /// <summary>
    /// Обязательные поля (для type = object)
    /// </summary>
    [JsonProperty("required")]
    public List<string>? Required { get; set; }

    /// <summary>
    /// Элементы массива (для type = array)
    /// </summary>
    [JsonProperty("items")]
    public FunctionParameter? Items { get; set; }
}

/// <summary>
/// Модель функции для OpenAI
/// </summary>
public class OpenAIFunction
{
    /// <summary>
    /// Название функции
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Описание функции
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Параметры функции
    /// </summary>
    [JsonProperty("parameters")]
    public FunctionParameter Parameters { get; set; } = new();
}

/// <summary>
/// Модель запроса к OpenAI API
/// </summary>
public class OpenAIRequest
{
    /// <summary>
    /// Модель для использования
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Список сообщений
    /// </summary>
    [JsonProperty("messages")]
    public List<OpenAIMessage> Messages { get; set; } = new();

    /// <summary>
    /// Доступные функции
    /// </summary>
    [JsonProperty("functions")]
    public List<OpenAIFunction>? Functions { get; set; }

    /// <summary>
    /// Как вызывать функции (auto, none, или {name: "function_name"})
    /// </summary>
    [JsonProperty("function_call")]
    public object? FunctionCall { get; set; }

    /// <summary>
    /// Максимальное количество токенов в ответе
    /// </summary>
    [JsonProperty("max_tokens")]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Температура для генерации (0.0 - 2.0)
    /// </summary>
    [JsonProperty("temperature")]
    public float? Temperature { get; set; }
}

/// <summary>
/// Модель выбора в ответе OpenAI
/// </summary>
public class OpenAIChoice
{
    /// <summary>
    /// Индекс выбора
    /// </summary>
    [JsonProperty("index")]
    public int Index { get; set; }

    /// <summary>
    /// Сообщение от ассистента
    /// </summary>
    [JsonProperty("message")]
    public OpenAIMessage? Message { get; set; }

    /// <summary>
    /// Причина завершения генерации
    /// </summary>
    [JsonProperty("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Модель использования токенов
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Количество токенов в промпте
    /// </summary>
    [JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Количество токенов в ответе
    /// </summary>
    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Общее количество токенов
    /// </summary>
    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Модель ответа OpenAI API
/// </summary>
public class OpenAIResponse
{
    /// <summary>
    /// Идентификатор запроса
    /// </summary>
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Тип объекта (обычно "chat.completion")
    /// </summary>
    [JsonProperty("object")]
    public string? Object { get; set; }

    /// <summary>
    /// Время создания ответа
    /// </summary>
    [JsonProperty("created")]
    public long Created { get; set; }

    /// <summary>
    /// Модель, которая использовалась
    /// </summary>
    [JsonProperty("model")]
    public string? Model { get; set; }

    /// <summary>
    /// Список вариантов ответа
    /// </summary>
    [JsonProperty("choices")]
    public List<OpenAIChoice> Choices { get; set; } = new();

    /// <summary>
    /// Использование токенов
    /// </summary>
    [JsonProperty("usage")]
    public TokenUsage? Usage { get; set; }
}

/// <summary>
/// Модель ошибки OpenAI API
/// </summary>
public class OpenAIError
{
    /// <summary>
    /// Тип ошибки
    /// </summary>
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Код ошибки
    /// </summary>
    [JsonProperty("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Сообщение об ошибке
    /// </summary>
    [JsonProperty("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Модель ответа с ошибкой OpenAI API
/// </summary>
public class OpenAIErrorResponse
{
    /// <summary>
    /// Информация об ошибке
    /// </summary>
    [JsonProperty("error")]
    public OpenAIError? Error { get; set; }
}