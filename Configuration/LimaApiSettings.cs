namespace LimaVoiceAssistant.Configuration;

/// <summary>
/// Настройки для Lima API
/// </summary>
public class LimaApiSettings
{
    /// <summary>
    /// Базовый URL API Lima
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.lima.uz";

    /// <summary>
    /// JWT токен для авторизации
    /// </summary>
    public string JwtToken { get; set; } = string.Empty;
}

/// <summary>
/// Настройки для OpenAI API
/// </summary>
public class OpenAISettings
{
    /// <summary>
    /// API ключ OpenAI
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Модель для использования
    /// </summary>
    public string Model { get; set; } = "gpt-4";

    /// <summary>
    /// Максимальное количество токенов в ответе
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Температура для генерации ответов (0.0 - 1.0)
    /// </summary>
    public float Temperature { get; set; } = 0.1f;
}