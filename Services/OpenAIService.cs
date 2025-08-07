using LimaVoiceAssistant.Configuration;
using LimaVoiceAssistant.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;
using System.Text;

namespace LimaVoiceAssistant.Services;

/// <summary>
/// Сервис для работы с OpenAI API и Function Calling
/// </summary>
public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAISettings _settings;
    private readonly ILimaFunctionsService _limaFunctionsService;
    private readonly IConversationContextService _contextService;
    private readonly NLog.ILogger _logger;

    public OpenAIService(
        HttpClient httpClient,
        IOptions<OpenAISettings> settings,
        ILimaFunctionsService limaFunctionsService,
        IConversationContextService contextService)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _limaFunctionsService = limaFunctionsService;
        _contextService = contextService;
        _logger = LogManager.GetCurrentClassLogger();

        // Настройка HTTP клиента для OpenAI
        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LimaVoiceAssistant/1.0");
    }

    /// <summary>
    /// Отправка сообщения в OpenAI с поддержкой Function Calling
    /// </summary>
    public async Task<OpenAIResponse> ChatCompletionAsync(List<OpenAIMessage> messages, 
        List<OpenAIFunction>? functions = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Отправка запроса в OpenAI с {messages.Count} сообщениями");

            var request = new OpenAIRequest
            {
                Model = _settings.Model,
                Messages = messages,
                Functions = functions,
                MaxTokens = _settings.MaxTokens,
                Temperature = _settings.Temperature
            };

            // Если есть функции, включаем автовызов
            if (functions != null && functions.Count > 0)
            {
                request.FunctionCall = "auto";
            }

            var json = JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("v1/chat/completions", content, cancellationToken);

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Ошибка OpenAI API: {response.StatusCode}, {responseJson}");
                var errorResponse = JsonConvert.DeserializeObject<OpenAIErrorResponse>(responseJson);
                throw new HttpRequestException($"OpenAI API Error: {errorResponse?.Error?.Message ?? "Unknown error"}");
            }

            var result = JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
            _logger.Info($"Получен ответ от OpenAI: {result?.Choices?.Count ?? 0} вариантов");

            return result ?? new OpenAIResponse();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при вызове OpenAI API");
            throw;
        }
    }

    /// <summary>
    /// Получение списка всех доступных функций для голосового помощника
    /// </summary>
    public List<OpenAIFunction> GetAvailableFunctions()
    {
        return new List<OpenAIFunction>
        {
            // Функция №1: Создание брони в аптеку
            new OpenAIFunction
            {
                Name = "createPharmacyReservation",
                Description = "Создание брони препаратов в аптеку. Используется когда пользователь хочет создать заказ, бронь или купить препараты в аптеке.",
                Parameters = new FunctionParameter
                {
                    Type = "object",
                    Properties = new Dictionary<string, FunctionParameter>
                    {
                        ["pharmacyName"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Название аптеки"
                        },
                        ["drugs"] = new FunctionParameter
                        {
                            Type = "array",
                            Description = "Список препаратов с количеством",
                            Items = new FunctionParameter
                            {
                                Type = "object",
                                Properties = new Dictionary<string, FunctionParameter>
                                {
                                    ["drugName"] = new FunctionParameter { Type = "string", Description = "Название препарата" },
                                    ["quantity"] = new FunctionParameter { Type = "integer", Description = "Количество упаковок" }
                                },
                                Required = new List<string> { "drugName", "quantity" }
                            }
                        },
                        ["prepaymentPercent"] = new FunctionParameter
                        {
                            Type = "number",
                            Description = "Процент предоплаты (по умолчанию 100)"
                        },
                        ["paymentType"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Тип оплаты",
                            Enum = new List<string> { "наличные", "перечисление" }
                        },
                        ["comment"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Комментарий к заказу"
                        }
                    },
                    Required = new List<string> { "pharmacyName", "drugs" }
                }
            },

            // Функция №2: Создание визита в ЛПУ
            new OpenAIFunction
            {
                Name = "createClinicVisit",
                Description = "Фиксация визита в ЛПУ (клинику, больницу). Используется когда пользователь сообщает о визите к врачу, презентации препаратов.",
                Parameters = new FunctionParameter
                {
                    Type = "object",
                    Properties = new Dictionary<string, FunctionParameter>
                    {
                        ["clinicName"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Название клиники, ЛПУ, больницы"
                        },
                        ["doctorName"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "ФИО врача"
                        },
                        ["discussedDrugs"] = new FunctionParameter
                        {
                            Type = "array",
                            Description = "Список препаратов, которые презентовали или обсуждали",
                            Items = new FunctionParameter { Type = "string" }
                        },
                        ["latitude"] = new FunctionParameter
                        {
                            Type = "number",
                            Description = "Широта местоположения"
                        },
                        ["longitude"] = new FunctionParameter
                        {
                            Type = "number",
                            Description = "Долгота местоположения"
                        },
                        ["comment"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Комментарий к визиту"
                        }
                    },
                    Required = new List<string> { "clinicName", "discussedDrugs" }
                }
            },

            // Функция №3: Получение истории визитов
            new OpenAIFunction
            {
                Name = "getVisitHistory",
                Description = "Получение истории визитов и заказов пользователя. Используется для просмотра прошлых визитов, заказов, активности.",
                Parameters = new FunctionParameter
                {
                    Type = "object",
                    Properties = new Dictionary<string, FunctionParameter>
                    {
                        ["visitType"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Тип визита для фильтрации",
                            Enum = new List<string> { "аптека", "лпу", "все" }
                        },
                        ["organizationName"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Название организации для фильтрации"
                        },
                        ["page"] = new FunctionParameter
                        {
                            Type = "integer",
                            Description = "Номер страницы (по умолчанию 1)"
                        }
                    }
                }
            },

            // Функция №4: Поиск организаций
            new OpenAIFunction
            {
                Name = "searchOrganizations",
                Description = "Поиск аптек, клиник, ЛПУ по названию. Используется когда нужно найти контакты, адрес организации.",
                Parameters = new FunctionParameter
                {
                    Type = "object",
                    Properties = new Dictionary<string, FunctionParameter>
                    {
                        ["organizationName"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Название организации для поиска"
                        }
                    },
                    Required = new List<string> { "organizationName" }
                }
            },

            // Функция №5: Просмотр плана визитов
            new OpenAIFunction
            {
                Name = "getPlannedVisits",
                Description = "Получение плана запланированных визитов на определенную дату или месяц.",
                Parameters = new FunctionParameter
                {
                    Type = "object",
                    Properties = new Dictionary<string, FunctionParameter>
                    {
                        ["date"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Дата или день недели (например: 'сегодня', 'завтра', 'пятница', '2025-08-08')"
                        },
                        ["viewType"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Тип просмотра",
                            Enum = new List<string> { "день", "месяц", "неделя" }
                        }
                    }
                }
            },

            // Дополнительная функция: Проверка остатков препарата
            new OpenAIFunction
            {
                Name = "getDrugStock",
                Description = "Проверка остатков препарата на складе. Используется когда спрашивают о наличии, остатках, количестве препарата. Если drugName не указан, показывает все остатки.",
                Parameters = new FunctionParameter
                {
                    Type = "object",
                    Properties = new Dictionary<string, FunctionParameter>
                    {
                        ["drugName"] = new FunctionParameter
                        {
                            Type = "string",
                            Description = "Название препарата для проверки остатков. Если не указано, показывает остатки всех препаратов."
                        }
                    },
                    Required = new List<string>()
                }
            }
        };
    }

    /// <summary>
    /// Обработка результата вызова функции и формирование ответа пользователю
    /// </summary>
    public async Task<string> ExecuteFunctionAsync(string functionName, string functionArguments, 
        string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Выполнение функции '{functionName}' с аргументами: {functionArguments}");

            var arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(functionArguments) 
                           ?? new Dictionary<string, object>();

            switch (functionName)
            {
                case "createPharmacyReservation":
                    return await ExecuteCreatePharmacyReservation(arguments);

                case "createClinicVisit":
                    return await ExecuteCreateClinicVisit(arguments);

                case "getVisitHistory":
                    return await ExecuteGetVisitHistory(arguments);

                case "searchOrganizations":
                    return await ExecuteSearchOrganizations(arguments);

                case "getPlannedVisits":
                    return await ExecuteGetPlannedVisits(arguments);

                case "getDrugStock":
                    return await ExecuteGetDrugStock(arguments);

                default:
                    _logger.Warn($"Неизвестная функция: {functionName}");
                    return $"❌ Функция '{functionName}' не поддерживается.";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при выполнении функции '{functionName}'");
            return $"❌ Произошла ошибка при выполнении операции: {ex.Message}";
        }
    }

    /// <summary>
    /// Обработка текстового сообщения пользователя
    /// </summary>
    public async Task<AssistantResponse> ProcessUserMessageAsync(string userMessage, string sessionId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"Обработка сообщения пользователя: '{userMessage}' (сессия: {sessionId})");

            // Проверяем команды сброса
            if (_contextService.IsResetCommand(userMessage))
            {
                await _contextService.ClearContextAsync(sessionId);
                return new AssistantResponse
                {
                    Success = true,
                    Response = "✅ Контекст очищен. Чем могу помочь?",
                    ContextCleared = true
                };
            }

            // Добавляем сообщение пользователя в контекст
            await _contextService.AddMessageAsync(sessionId, "user", userMessage);

            // Получаем историю сообщений для OpenAI
            var messages = await _contextService.GetOpenAIMessagesAsync(sessionId);
            
            // Добавляем текущее сообщение пользователя
            messages.Add(new OpenAIMessage
            {
                Role = "user",
                Content = userMessage
            });

            // Получаем доступные функции
            var functions = GetAvailableFunctions();

            // Отправляем запрос в OpenAI
            var openAiResponse = await ChatCompletionAsync(messages.Cast<OpenAIMessage>().ToList(), functions, cancellationToken);

            if (openAiResponse.Choices.Count == 0)
            {
                return new AssistantResponse
                {
                    Success = false,
                    Response = "❌ Не получен ответ от системы. Попробуйте переформулировать запрос."
                };
            }

            var choice = openAiResponse.Choices.First();
            var assistantMessage = choice.Message;

            // Если OpenAI вызвал функцию
            if (assistantMessage?.FunctionCall != null)
            {
                var functionName = assistantMessage.FunctionCall.Name;
                var functionArguments = assistantMessage.FunctionCall.Arguments;

                // Выполняем функцию
                var functionResult = await ExecuteFunctionAsync(functionName, functionArguments, sessionId, cancellationToken);

                // Сохраняем в контекст вызов функции и результат
                await _contextService.AddMessageAsync(sessionId, "assistant", "", functionName, functionArguments);
                await _contextService.AddMessageAsync(sessionId, "function", functionResult, functionName);

                return new AssistantResponse
                {
                    Success = true,
                    Response = functionResult,
                    FunctionName = functionName
                };
            }
            // Если OpenAI ответил обычным текстом
            else if (!string.IsNullOrEmpty(assistantMessage?.Content))
            {
                await _contextService.AddMessageAsync(sessionId, "assistant", assistantMessage.Content);

                return new AssistantResponse
                {
                    Success = true,
                    Response = assistantMessage.Content
                };
            }

            return new AssistantResponse
            {
                Success = false,
                Response = "❌ Не удалось обработать запрос. Попробуйте переформулировать."
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при обработке сообщения пользователя: '{userMessage}'");
            
            return new AssistantResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                Response = "❌ Произошла внутренняя ошибка. Попробуйте позже."
            };
        }
    }

    #region Методы выполнения функций

    private async Task<string> ExecuteCreatePharmacyReservation(Dictionary<string, object> arguments)
    {
        var pharmacyName = arguments.GetValueOrDefault("pharmacyName")?.ToString() ?? "";
        var prepaymentPercent = Convert.ToDecimal(arguments.GetValueOrDefault("prepaymentPercent") ?? 100);
        var paymentType = arguments.GetValueOrDefault("paymentType")?.ToString() ?? "наличные";
        var comment = arguments.GetValueOrDefault("comment")?.ToString();

        var drugs = new List<DrugOrderItem>();
        if (arguments.ContainsKey("drugs") && arguments["drugs"] is Newtonsoft.Json.Linq.JArray drugsArray)
        {
            foreach (var drug in drugsArray)
            {
                if (drug is Newtonsoft.Json.Linq.JObject drugObj)
                {
                    drugs.Add(new DrugOrderItem
                    {
                        DrugName = drugObj["drugName"]?.ToString() ?? "",
                        Quantity = drugObj["quantity"]?.ToObject<int>() ?? 1
                    });
                }
            }
        }

        return await _limaFunctionsService.CreatePharmacyReservationAsync(
            pharmacyName, drugs, prepaymentPercent, paymentType, comment);
    }

    private async Task<string> ExecuteCreateClinicVisit(Dictionary<string, object> arguments)
    {
        var clinicName = arguments.GetValueOrDefault("clinicName")?.ToString() ?? "";
        var doctorName = arguments.GetValueOrDefault("doctorName")?.ToString();
        double? latitude = arguments.ContainsKey("latitude") ? Convert.ToDouble(arguments["latitude"]) : null;
        double? longitude = arguments.ContainsKey("longitude") ? Convert.ToDouble(arguments["longitude"]) : null;
        var comment = arguments.GetValueOrDefault("comment")?.ToString();

        var discussedDrugs = new List<string>();
        if (arguments.ContainsKey("discussedDrugs") && arguments["discussedDrugs"] is Newtonsoft.Json.Linq.JArray drugsArray)
        {
            discussedDrugs.AddRange(drugsArray.Select(drug => drug.ToString()));
        }

        return await _limaFunctionsService.CreateClinicVisitAsync(
            clinicName, doctorName, discussedDrugs, latitude, longitude, comment);
    }

    private async Task<string> ExecuteGetVisitHistory(Dictionary<string, object> arguments)
    {
        var visitType = arguments.GetValueOrDefault("visitType")?.ToString();
        var organizationName = arguments.GetValueOrDefault("organizationName")?.ToString();
        var page = Convert.ToInt32(arguments.GetValueOrDefault("page") ?? 1);
        var date = arguments.GetValueOrDefault("date")?.ToString();

        return await _limaFunctionsService.GetVisitHistoryAsync(visitType, organizationName, page, date);
    }

    private async Task<string> ExecuteSearchOrganizations(Dictionary<string, object> arguments)
    {
        var organizationName = arguments.GetValueOrDefault("organizationName")?.ToString() ?? "";
        return await _limaFunctionsService.SearchOrganizationsAsync(organizationName);
    }

    private async Task<string> ExecuteGetPlannedVisits(Dictionary<string, object> arguments)
    {
        var date = arguments.GetValueOrDefault("date")?.ToString();
        var viewType = arguments.GetValueOrDefault("viewType")?.ToString() ?? "день";

        return await _limaFunctionsService.GetPlannedVisitsAsync(date, viewType);
    }

    private async Task<string> ExecuteGetDrugStock(Dictionary<string, object> arguments)
    {
        var drugName = arguments.GetValueOrDefault("drugName")?.ToString();
        return await _limaFunctionsService.GetDrugStockAsync(drugName);
    }

    #endregion
}