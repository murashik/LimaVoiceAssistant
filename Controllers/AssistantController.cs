using LimaVoiceAssistant.Models;
using LimaVoiceAssistant.Services;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace LimaVoiceAssistant.Controllers;

/// <summary>
/// Контроллер для работы с голосовым помощником
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AssistantController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly NLog.ILogger _logger;

    public AssistantController(IOpenAIService openAIService)
    {
        _openAIService = openAIService;
        _logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Основной эндпоинт для обработки текстовых запросов пользователя
    /// </summary>
    /// <param name="request">Запрос с текстовым сообщением пользователя</param>
    /// <returns>Ответ голосового помощника</returns>
    [HttpPost("query")]
    public async Task<ActionResult<AssistantResponse>> ProcessQuery([FromBody] AssistantRequest request)
    {
        try
        {
            _logger.Info($"Получен запрос: '{request.Message}' (сессия: {request.SessionId ?? "новая"})");

            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new AssistantResponse
                {
                    Success = false,
                    ErrorMessage = "Сообщение не может быть пустым",
                    Response = "❌ Пожалуйста, скажите что-нибудь."
                });
            }

            // Генерируем sessionId если не передан
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            // Передаем обработку в OpenAI Service
            var response = await _openAIService.ProcessUserMessageAsync(
                request.Message, 
                sessionId, 
                HttpContext.RequestAborted);

            _logger.Info($"Ответ сформирован: функция='{response.FunctionName}', успех={response.Success}");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при обработке запроса: '{request.Message}'");
            
            return Ok(new AssistantResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                Response = "❌ Произошла внутренняя ошибка. Попробуйте позже или обратитесь к администратору."
            });
        }
    }

    /// <summary>
    /// Получение справки по доступным командам
    /// </summary>
    /// <returns>Справка по использованию</returns>
    [HttpGet("help")]
    public ActionResult<string> GetHelp()
    {
        return Ok(@"🤖 Голосовой помощник Lima готов помочь!

📝 **Примеры команд:**

🏪 **Создание брони в аптеку:**
   ""Создай бронь в аптеку Нурафшон на Парацетамол — 5 упаковок""

🏥 **Фиксация визита в ЛПУ:**
   ""Зашёл в клинику МедиГранд, говорил с врачом Ивановым о Парацетамоле""

📋 **История визитов:**
   ""Покажи мои визиты"", ""История визитов в аптеки""

🔍 **Поиск организаций:**
   ""Найди аптеку Нурафшон"", ""Где находится клиника МедиГранд""

📅 **План визитов:**
   ""Какие визиты на пятницу?"", ""План на месяц""

💊 **Остатки препаратов:**
   ""Сколько Парацетамола?"", ""Есть ли Ибупрофен?""

❌ **Отмена:** ""Отмена"", ""Очисть контекст""

Просто скажите что вам нужно, и я помогу! ✨");
    }
}