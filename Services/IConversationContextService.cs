using LimaVoiceAssistant.Models;

namespace LimaVoiceAssistant.Services;

/// <summary>
/// Интерфейс сервиса для управления контекстом диалога
/// </summary>
public interface IConversationContextService
{
    /// <summary>
    /// Получение контекста диалога по ID сессии
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>Контекст диалога</returns>
    Task<ConversationContext> GetContextAsync(string sessionId);

    /// <summary>
    /// Сохранение контекста диалога
    /// </summary>
    /// <param name="context">Контекст для сохранения</param>
    /// <returns>Задача сохранения</returns>
    Task SaveContextAsync(ConversationContext context);

    /// <summary>
    /// Очистка контекста диалога
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>Задача очистки</returns>
    Task ClearContextAsync(string sessionId);

    /// <summary>
    /// Добавление сообщения в контекст
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="role">Роль отправителя (user, assistant, system)</param>
    /// <param name="content">Содержание сообщения</param>
    /// <param name="functionName">Название функции (опционально)</param>
    /// <param name="functionArguments">Аргументы функции (опционально)</param>
    /// <returns>Обновлённый контекст</returns>
    Task<ConversationContext> AddMessageAsync(string sessionId, string role, string content, 
        string? functionName = null, string? functionArguments = null);

    /// <summary>
    /// Установка незавершённой операции
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="operationType">Тип операции</param>
    /// <param name="parameters">Параметры операции</param>
    /// <param name="missingParameters">Недостающие параметры</param>
    /// <param name="nextQuestion">Следующий вопрос пользователю</param>
    /// <returns>Обновлённый контекст</returns>
    Task<ConversationContext> SetPendingOperationAsync(string sessionId, string operationType, 
        Dictionary<string, object> parameters, List<string> missingParameters, string? nextQuestion = null);

    /// <summary>
    /// Получение незавершённой операции
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>Незавершённая операция или null</returns>
    Task<PendingOperation?> GetPendingOperationAsync(string sessionId);

    /// <summary>
    /// Обновление параметров незавершённой операции
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="newParameters">Новые параметры</param>
    /// <returns>Обновлённый контекст</returns>
    Task<ConversationContext> UpdatePendingOperationParametersAsync(string sessionId, Dictionary<string, object> newParameters);

    /// <summary>
    /// Завершение незавершённой операции
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>Обновлённый контекст</returns>
    Task<ConversationContext> CompletePendingOperationAsync(string sessionId);

    /// <summary>
    /// Проверка, содержит ли сообщение команды отмены или очистки
    /// </summary>
    /// <param name="message">Сообщение пользователя</param>
    /// <returns>True, если сообщение содержит команду отмены</returns>
    bool IsResetCommand(string message);

    /// <summary>
    /// Получение истории сообщений для OpenAI API
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="maxMessages">Максимальное количество сообщений</param>
    /// <returns>Список сообщений в формате OpenAI</returns>
    Task<List<OpenAIMessage>> GetOpenAIMessagesAsync(string sessionId, int maxMessages = 10);

    /// <summary>
    /// Очистка устаревших контекстов
    /// </summary>
    /// <param name="olderThan">Временной порог для удаления</param>
    /// <returns>Количество удалённых контекстов</returns>
    Task<int> CleanupOldContextsAsync(TimeSpan olderThan);
}