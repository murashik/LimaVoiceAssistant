using LimaVoiceAssistant.Models;
using Newtonsoft.Json;
using NLog;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LimaVoiceAssistant.Services;

/// <summary>
/// Сервис для управления контекстом диалога (in-memory хранилище с возможностью расширения)
/// </summary>
public class ConversationContextService : IConversationContextService
{
    private readonly NLog.ILogger _logger;
    private readonly ConcurrentDictionary<string, ConversationContext> _contexts;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _contextExpiry = TimeSpan.FromHours(2); // Контекст живёт 2 часа

    // Регулярные выражения для определения команд отмены/очистки
    private readonly Regex[] _resetCommands = new[]
    {
        new Regex(@"\b(отмена|отменить|cancel)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(очисть|очистить|clear|reset)\s*(контекст|context)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(сначала|заново|restart)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"\b(стоп|stop)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public ConversationContextService()
    {
        _logger = LogManager.GetCurrentClassLogger();
        _contexts = new ConcurrentDictionary<string, ConversationContext>();
        
        // Таймер для периодической очистки устаревших контекстов (каждые 30 минут)
        _cleanupTimer = new Timer(async _ => await CleanupOldContextsAsync(_contextExpiry), 
            null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
        
        _logger.Info("Сервис контекста диалога инициализирован");
    }

    /// <summary>
    /// Получение контекста диалога по ID сессии
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>Контекст диалога</returns>
    public async Task<ConversationContext> GetContextAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            _logger.Info($"Создан новый ID сессии: {sessionId}");
        }

        if (_contexts.TryGetValue(sessionId, out var existingContext))
        {
            _logger.Debug($"Получен существующий контекст для сессии: {sessionId}");
            return existingContext;
        }

        var newContext = new ConversationContext
        {
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        _contexts[sessionId] = newContext;
        _logger.Info($"Создан новый контекст для сессии: {sessionId}");
        
        return await Task.FromResult(newContext);
    }

    /// <summary>
    /// Сохранение контекста диалога
    /// </summary>
    /// <param name="context">Контекст для сохранения</param>
    /// <returns>Задача сохранения</returns>
    public async Task SaveContextAsync(ConversationContext context)
    {
        if (context == null || string.IsNullOrWhiteSpace(context.SessionId))
        {
            _logger.Warn("Попытка сохранить пустой контекст или контекст без SessionId");
            return;
        }

        context.LastUpdated = DateTime.UtcNow;
        _contexts[context.SessionId] = context;
        
        _logger.Debug($"Контекст сохранён для сессии: {context.SessionId}");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Очистка контекста диалога
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>Задача очистки</returns>
    public async Task ClearContextAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.Warn("Попытка очистить контекст с пустым SessionId");
            return;
        }

        if (_contexts.TryRemove(sessionId, out var removedContext))
        {
            _logger.Info($"Контекст очищен для сессии: {sessionId}");
        }
        else
        {
            _logger.Debug($"Контекст не найден для очистки сессии: {sessionId}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Добавление сообщения в контекст
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="role">Роль отправителя (user, assistant, system)</param>
    /// <param name="content">Содержание сообщения</param>
    /// <param name="functionName">Название функции (опционально)</param>
    /// <param name="functionArguments">Аргументы функции (опционально)</param>
    /// <returns>Обновлённый контекст</returns>
    public async Task<ConversationContext> AddMessageAsync(string sessionId, string role, string content, 
        string? functionName = null, string? functionArguments = null)
    {
        var context = await GetContextAsync(sessionId);

        var message = new ConversationMessage
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            FunctionName = functionName,
            FunctionArguments = functionArguments
        };

        context.Messages.Add(message);

        // Ограничиваем историю последними 50 сообщениями для экономии памяти
        if (context.Messages.Count > 50)
        {
            context.Messages.RemoveRange(0, context.Messages.Count - 50);
            _logger.Debug($"Обрезана история сообщений для сессии: {sessionId}");
        }

        await SaveContextAsync(context);
        
        _logger.Debug($"Добавлено сообщение от {role} в сессию {sessionId}");
        return context;
    }

    /// <summary>
    /// Установка незавершённой операции
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="operationType">Тип операции</param>
    /// <param name="parameters">Параметры операции</param>
    /// <param name="missingParameters">Недостающие параметры</param>
    /// <param name="nextQuestion">Следующий вопрос пользователю</param>
    /// <returns>Обновлённый контекст</returns>
    public async Task<ConversationContext> SetPendingOperationAsync(string sessionId, string operationType, 
        Dictionary<string, object> parameters, List<string> missingParameters, string? nextQuestion = null)
    {
        var context = await GetContextAsync(sessionId);

        context.PendingOperation = new PendingOperation
        {
            OperationType = operationType,
            Parameters = parameters ?? new Dictionary<string, object>(),
            MissingParameters = missingParameters ?? new List<string>(),
            NextQuestion = nextQuestion,
            CreatedAt = DateTime.UtcNow
        };

        await SaveContextAsync(context);
        
        _logger.Info($"Установлена незавершённая операция '{operationType}' для сессии: {sessionId}");
        return context;
    }

    /// <summary>
    /// Получение незавершённой операции
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>Незавершённая операция или null</returns>
    public async Task<PendingOperation?> GetPendingOperationAsync(string sessionId)
    {
        var context = await GetContextAsync(sessionId);
        return context.PendingOperation;
    }

    /// <summary>
    /// Обновление параметров незавершённой операции
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="newParameters">Новые параметры</param>
    /// <returns>Обновлённый контекст</returns>
    public async Task<ConversationContext> UpdatePendingOperationParametersAsync(string sessionId, Dictionary<string, object> newParameters)
    {
        var context = await GetContextAsync(sessionId);

        if (context.PendingOperation != null && newParameters != null)
        {
            foreach (var param in newParameters)
            {
                context.PendingOperation.Parameters[param.Key] = param.Value;
            }

            await SaveContextAsync(context);
            _logger.Debug($"Обновлены параметры незавершённой операции для сессии: {sessionId}");
        }

        return context;
    }

    /// <summary>
    /// Завершение незавершённой операции
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <returns>Обновлённый контекст</returns>
    public async Task<ConversationContext> CompletePendingOperationAsync(string sessionId)
    {
        var context = await GetContextAsync(sessionId);

        if (context.PendingOperation != null)
        {
            var operationType = context.PendingOperation.OperationType;
            context.PendingOperation = null;
            
            await SaveContextAsync(context);
            _logger.Info($"Завершена незавершённая операция '{operationType}' для сессии: {sessionId}");
        }

        return context;
    }

    /// <summary>
    /// Проверка, содержит ли сообщение команды отмены или очистки
    /// </summary>
    /// <param name="message">Сообщение пользователя</param>
    /// <returns>True, если сообщение содержит команду отмены</returns>
    public bool IsResetCommand(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var normalizedMessage = message.Trim().ToLowerInvariant();
        
        foreach (var regex in _resetCommands)
        {
            if (regex.IsMatch(normalizedMessage))
            {
                _logger.Debug($"Обнаружена команда сброса в сообщении: '{message}'");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Получение истории сообщений для OpenAI API
    /// </summary>
    /// <param name="sessionId">Идентификатор сессии</param>
    /// <param name="maxMessages">Максимальное количество сообщений</param>
    /// <returns>Список сообщений в формате OpenAI</returns>
    public async Task<List<OpenAIMessage>> GetOpenAIMessagesAsync(string sessionId, int maxMessages = 10)
    {
        var context = await GetContextAsync(sessionId);
        var messages = new List<OpenAIMessage>();

        // Добавляем системное сообщение
        messages.Add(new OpenAIMessage
        {
            Role = "system",
            Content = "Ты голосовой помощник для медицинской CRM-системы Lima. Помогаешь медпредставителям создавать брони в аптеки, визиты в ЛПУ, получать информацию об остатках препаратов и планах визитов. Отвечай кратко и по существу. Используй доступные функции для выполнения запросов пользователя."
        });

        // Добавляем последние сообщения из истории
        var recentMessages = context.Messages
            .OrderByDescending(m => m.Timestamp)
            .Take(maxMessages - 1) // -1 для системного сообщения
            .Reverse()
            .ToList();

        foreach (var msg in recentMessages)
        {
            var openAiMessage = new OpenAIMessage
            {
                Role = msg.Role,
                Content = msg.Content
            };

            // Для сообщений с ролью 'function' требуется параметр 'name'
            if (msg.Role == "function" && !string.IsNullOrEmpty(msg.FunctionName))
            {
                openAiMessage.Name = msg.FunctionName;
            }
            // Для сообщений от ассистента с вызовом функции
            else if (!string.IsNullOrEmpty(msg.FunctionName) && !string.IsNullOrEmpty(msg.FunctionArguments))
            {
                openAiMessage.FunctionCall = new FunctionCall
                {
                    Name = msg.FunctionName,
                    Arguments = msg.FunctionArguments ?? "{}"
                };
            }

            messages.Add(openAiMessage);
        }

        return messages;
    }

    /// <summary>
    /// Очистка устаревших контекстов
    /// </summary>
    /// <param name="olderThan">Временной порог для удаления</param>
    /// <returns>Количество удалённых контекстов</returns>
    public async Task<int> CleanupOldContextsAsync(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.UtcNow - olderThan;
        var expiredSessions = new List<string>();

        foreach (var kvp in _contexts)
        {
            if (kvp.Value.LastUpdated < cutoffTime)
            {
                expiredSessions.Add(kvp.Key);
            }
        }

        var removedCount = 0;
        foreach (var sessionId in expiredSessions)
        {
            if (_contexts.TryRemove(sessionId, out _))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.Info($"Очищено устаревших контекстов: {removedCount}");
        }

        return await Task.FromResult(removedCount);
    }

    /// <summary>
    /// Освобождение ресурсов
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _contexts.Clear();
        _logger.Info("Сервис контекста диалога завершён");
    }
}