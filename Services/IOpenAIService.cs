using LimaVoiceAssistant.Models;

namespace LimaVoiceAssistant.Services;

/// <summary>
/// Интерфейс сервиса для работы с OpenAI API и Function Calling
/// </summary>
public interface IOpenAIService
{
    /// <summary>
    /// Отправка сообщения в OpenAI с поддержкой Function Calling
    /// </summary>
    /// <param name="messages">Список сообщений диалога</param>
    /// <param name="functions">Доступные функции для вызова</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ от OpenAI</returns>
    Task<OpenAIResponse> ChatCompletionAsync(List<OpenAIMessage> messages, 
        List<OpenAIFunction>? functions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение списка всех доступных функций для голосового помощника
    /// </summary>
    /// <returns>Список функций</returns>
    List<OpenAIFunction> GetAvailableFunctions();

    /// <summary>
    /// Обработка результата вызова функции и формирование ответа пользователю
    /// </summary>
    /// <param name="functionName">Название функции</param>
    /// <param name="functionArguments">Аргументы функции в JSON формате</param>
    /// <param name="sessionId">ID сессии для контекста</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат выполнения функции</returns>
    Task<string> ExecuteFunctionAsync(string functionName, string functionArguments, 
        string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обработка текстового сообщения пользователя
    /// </summary>
    /// <param name="userMessage">Сообщение пользователя</param>
    /// <param name="sessionId">ID сессии для контекста</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Ответ ассистента</returns>
    Task<AssistantResponse> ProcessUserMessageAsync(string userMessage, string sessionId, 
        CancellationToken cancellationToken = default);
}