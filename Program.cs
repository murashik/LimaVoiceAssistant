using LimaVoiceAssistant.Clients;
using LimaVoiceAssistant.Configuration;
using LimaVoiceAssistant.Services;
using NLog.Web;
using NLog;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Debug("Инициализация приложения LimaVoiceAssistant");

    var builder = WebApplication.CreateBuilder(args);

    // Конфигурация NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Настройка конфигурации
    builder.Services.Configure<LimaApiSettings>(
        builder.Configuration.GetSection("LimaApi"));
    builder.Services.Configure<OpenAISettings>(
        builder.Configuration.GetSection("OpenAI"));

    // HTTP клиенты
    builder.Services.AddHttpClient<ILimaApiClient, LimaApiClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    
    builder.Services.AddHttpClient<IOpenAIService, OpenAIService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    });

    // Регистрация сервисов
    builder.Services.AddScoped<ILimaApiClient, LimaApiClient>();
    builder.Services.AddScoped<IDrugSearchService, DrugSearchService>();
    builder.Services.AddSingleton<IConversationContextService, ConversationContextService>();
    builder.Services.AddScoped<ILimaFunctionsService, LimaFunctionsService>();
    builder.Services.AddScoped<IOpenAIService, OpenAIService>();

    // Контроллеры
    builder.Services.AddControllers()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        });

    // API документация
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() 
        { 
            Title = "Lima Voice Assistant API", 
            Version = "v1",
            Description = "API голосового помощника для медицинской CRM-системы Lima"
        });
        
        // Подключение XML комментариев для документации
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddCheck("lima-api", () => 
        {
            var limaSettings = builder.Configuration.GetSection("LimaApi").Get<LimaApiSettings>();
            return !string.IsNullOrEmpty(limaSettings?.JwtToken) ? 
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy() :
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Lima API token not configured");
        });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lima Voice Assistant API v1");
            c.RoutePrefix = string.Empty; // Swagger UI будет доступен на корневом пути
        });
    }

    // Middleware
    app.UseCors();
    //app.UseHttpsRedirection();
    app.UseRouting();

    // Health check endpoint
    app.MapHealthChecks("/health");

    // API контроллеры
    app.MapControllers();

    // Добавляем простую страницу статуса
    app.MapGet("/", () => new
    {
        service = "Lima Voice Assistant API",
        version = "1.0.0",
        status = "running",
        timestamp = DateTime.UtcNow,
        endpoints = new
        {
            main = "/api/assistant/query",
            health = "/health",
            swagger = "/swagger"
        }
    });

    logger.Info("Lima Voice Assistant API запущен успешно");
    
    await app.RunAsync();
}
catch (Exception exception)
{
    logger.Error(exception, "Ошибка при запуске приложения");
    throw;
}
finally
{
    LogManager.Shutdown();
}