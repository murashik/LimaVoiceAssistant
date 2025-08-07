using LimaVoiceAssistant.Clients;
using LimaVoiceAssistant.Models;
using NLog;
using System.Globalization;
using System.Text;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;

namespace LimaVoiceAssistant.Services;

/// <summary>
/// Сервис для выполнения основных функций Lima
/// </summary>
public class LimaFunctionsService : ILimaFunctionsService
{
    private readonly ILimaApiClient _limaApiClient;
    private readonly IDrugSearchService _drugSearchService;
    private readonly NLog.ILogger _logger;

    public LimaFunctionsService(ILimaApiClient limaApiClient, IDrugSearchService drugSearchService)
    {
        _limaApiClient = limaApiClient;
        _drugSearchService = drugSearchService;
        _logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Функция №1: Создание брони в аптеку
    /// </summary>
    public async Task<string> CreatePharmacyReservationAsync(string pharmacyName, List<DrugOrderItem> drugs, 
        decimal prepaymentPercent = 100, string paymentType = "наличные", string? comment = null)
    {
        try
        {
            _logger.Info($"Создание брони в аптеку '{pharmacyName}' на {drugs.Count} препаратов");

            // 1. Поиск аптеки
            var orgSearch = await _limaApiClient.SearchOrganizationsAsync(pharmacyName);
            var pharmacy = orgSearch.Result.FirstOrDefault(o => o.TypeName.Contains("аптека", StringComparison.OrdinalIgnoreCase));
            
            if (pharmacy == null)
            {
                return $"❌ Аптека '{pharmacyName}' не найдена. Проверьте название и попробуйте снова.";
            }

            // 2. Поиск препаратов и получение информации
            var orderDrugs = new List<OrderDrug>();
            var notFoundDrugs = new List<string>();

            foreach (var drugItem in drugs)
            {
                var foundDrug = await _drugSearchService.FindDrugInPriceListAsync(drugItem.DrugName);
                if (foundDrug != null)
                {
                    orderDrugs.Add(new OrderDrug
                    {
                        IncomeDetailingId = foundDrug.IncomeDetailingId,
                        DrugId = foundDrug.Drug.DrugId,
                        Package = drugItem.Quantity
                    });
                    drugItem.IsFound = true;
                    _logger.Info($"Препарат найден: '{foundDrug.Drug.DrugName}' ({drugItem.Quantity} уп.)");
                }
                else
                {
                    notFoundDrugs.Add(drugItem.DrugName);
                    _logger.Warn($"Препарат не найден: '{drugItem.DrugName}'");
                }
            }

            if (orderDrugs.Count == 0)
            {
                return $"❌ Ни один из указанных препаратов не найден в системе. Проверьте названия: {string.Join(", ", notFoundDrugs)}";
            }

            // 3. Получение маржи по проценту предоплаты
            var margins = await _limaApiClient.GetMarginsAsync();
            var selectedMargin = margins.FirstOrDefault(m => m.PrepaymentPercent == prepaymentPercent && m.Retail);
            
            if (selectedMargin == null)
            {
                selectedMargin = margins.FirstOrDefault(m => m.Retail);
                if (selectedMargin != null)
                {
                    _logger.Warn($"Маржа с предоплатой {prepaymentPercent}% не найдена, используется {selectedMargin.PrepaymentPercent}%");
                }
                else
                {
                    return "❌ Не найдены доступные варианты предоплаты для данной аптеки.";
                }
            }

            // 4. Определение типа оплаты (по умолчанию - перечисление)
            var paymentVariantId = !string.IsNullOrWhiteSpace(paymentType) && 
                                   paymentType.ToLowerInvariant().Contains("наличн") ? 2 : 1;

            // 5. Создание заявки
            var visitRequest = new CreateVisitRequest
            {
                OrganizationId = pharmacy.Id,
                VisitType = 1, // Аптека
                MarginId = selectedMargin.Id,
                IsWholesaler = false,
                Complete = true,
                PaymentVariantId = paymentVariantId,
                Comment = comment ?? "Голосовая бронь через ассистента",
                Drugs = orderDrugs
            };

            var success = await _limaApiClient.CreateVisitAsync(visitRequest);

            if (success)
            {
                var result = new StringBuilder();
                result.AppendLine("✅ Бронь успешно создана!");
                result.AppendLine($"🏪 Аптека: {pharmacy.Name}");
                result.AppendLine($"💰 Предоплата: {selectedMargin.PrepaymentPercent}%");
                result.AppendLine($"💳 Оплата: {(paymentVariantId == 2 ? "наличными" : "перечислением")}");
                result.AppendLine($"📦 Препараты ({orderDrugs.Count}):");
                
                foreach (var drug in drugs.Where(d => d.IsFound))
                {
                    result.AppendLine($"   • {drug.DrugName} — {drug.Quantity} уп.");
                }

                if (notFoundDrugs.Count > 0)
                {
                    result.AppendLine($"⚠️ Не найдены: {string.Join(", ", notFoundDrugs)}");
                }

                return result.ToString();
            }
            else
            {
                return "❌ Ошибка при создании брони. Попробуйте позже или обратитесь к администратору.";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при создании брони в аптеку '{pharmacyName}'");
            return $"❌ Произошла ошибка при создании брони: {ex.Message}";
        }
    }

    /// <summary>
    /// Функция №2: Создание визита в ЛПУ
    /// </summary>
    public async Task<string> CreateClinicVisitAsync(string clinicName, string? doctorName, List<string> discussedDrugs, 
        double? latitude = null, double? longitude = null, string? comment = null)
    {
        try
        {
            _logger.Info($"Создание визита в ЛПУ '{clinicName}', врач: {doctorName ?? "не указан"}");

            // 1. Поиск ЛПУ
            var orgSearch = await _limaApiClient.SearchOrganizationsAsync(clinicName);
            var clinic = orgSearch.Result.FirstOrDefault(o => 
                !o.TypeName.Contains("аптека", StringComparison.OrdinalIgnoreCase));
            
            if (clinic == null)
            {
                return $"❌ ЛПУ '{clinicName}' не найдено. Проверьте название и попробуйте снова.";
            }

            // 2. Проверка обязательности указания врача
            if (string.IsNullOrWhiteSpace(doctorName))
            {
                return "❌ Для создания визита необходимо указать врача. Пожалуйста, назовите имя врача.";
            }

            // 3. Поиск врача с использованием FuzzySharp для нечёткого поиска
            int? doctorId = null;
            Doctor? foundDoctor = null;
            
            var doctors = await _limaApiClient.GetOrganizationDoctorsAsync(clinic.Id);
            
            if (doctors.Count > 0)
            {
                // Используем FuzzySharp для поиска наиболее похожего имени
                var bestMatch = Process.ExtractOne(doctorName, doctors.Select(d => d.FullName));
                
                // Если совпадение больше 70%, считаем что нашли врача
                if (bestMatch.Score >= 70)
                {
                    foundDoctor = doctors.First(d => d.FullName == bestMatch.Value);
                    doctorId = foundDoctor.Id;
                    _logger.Info($"Найден врач '{foundDoctor.FullName}' с точностью {bestMatch.Score}% для запроса '{doctorName}'");
                }
                else
                {
                    _logger.Warn($"Врач '{doctorName}' не найден в ЛПУ '{clinicName}'. Лучшее совпадение: '{bestMatch.Value}' ({bestMatch.Score}%)");
                }
            }
            

            // 4. Поиск препаратов компании
            var talkedAboutDrugs = new List<TalkedAboutDrug>();
            var notFoundDrugs = new List<string>();

            foreach (var drugName in discussedDrugs)
            {
                var foundDrug = await _drugSearchService.FindCompanyDrugAsync(drugName);
                if (foundDrug != null)
                {
                    talkedAboutDrugs.Add(new TalkedAboutDrug
                    {
                        DrugId = foundDrug.Id,
                        StatusId = null
                    });
                    _logger.Info($"Препарат найден для визита: '{foundDrug.Name}'");
                }
                else
                {
                    notFoundDrugs.Add(drugName);
                    _logger.Warn($"Препарат компании не найден: '{drugName}'");
                }
            }

            // 5. Создание визита
            var visitRequest = new CreateVisitRequest
            {
                OrganizationId = clinic.Id,
                VisitType = 2, // ЛПУ
                Complete = true,
                Latitude = latitude,
                Longitude = longitude,
                DoctorId = doctorId,
                Comment = comment ?? "Визит зафиксирован через голосового ассистента",
                TalkedAboutDrugs = talkedAboutDrugs
            };

            var success = await _limaApiClient.CreateVisitAsync(visitRequest);

            if (success)
            {
                var result = new StringBuilder();
                result.AppendLine("✅ Визит успешно зафиксирован!");
                result.AppendLine($"🏥 ЛПУ: {clinic.Name}");
                
                if (foundDoctor != null)
                {
                    result.AppendLine($"👨‍⚕️ Врач: {foundDoctor.FullName}");
                    if (!string.IsNullOrEmpty(foundDoctor.Position))
                        result.AppendLine($"📝 Должность: {foundDoctor.Position}");
                }
                else if (!string.IsNullOrWhiteSpace(doctorName))
                {
                    result.AppendLine($"👨‍⚕️ Врач: {doctorName} (не найден в базе)");
                }

                result.AppendLine($"💊 Презентованные препараты ({talkedAboutDrugs.Count}):");
                
                int index = 0;
                foreach (var drugName in discussedDrugs)
                {
                    var status = index < talkedAboutDrugs.Count ? "✅" : "❓";
                    result.AppendLine($"   {status} {drugName}");
                    index++;
                }

                if (notFoundDrugs.Count > 0)
                {
                    result.AppendLine($"⚠️ Препараты не из ассортимента компании: {string.Join(", ", notFoundDrugs)}");
                }

                return result.ToString();
            }
            else
            {
                return "❌ Ошибка при сохранении визита. Попробуйте позже или обратитесь к администратору.";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при создании визита в ЛПУ '{clinicName}'");
            return $"❌ Произошла ошибка при создании визита: {ex.Message}";
        }
    }

    /// <summary>
    /// Функция №3: Получение истории визитов и заказов
    /// </summary>
    public async Task<string> GetVisitHistoryAsync(string? visitType = null, string? organizationName = null, int page = 1, string? date = null)
    {
        try
        {
            _logger.Info($"Получение истории визитов: тип={visitType}, организация={organizationName}, страница={page}, дата={date}");

            // Определение типа визита
            int? typeId = null;
            if (!string.IsNullOrWhiteSpace(visitType))
            {
                if (visitType.Contains("аптек", StringComparison.OrdinalIgnoreCase))
                    typeId = 1;
                else if (visitType.Contains("лпу", StringComparison.OrdinalIgnoreCase) || 
                         visitType.Contains("клиник", StringComparison.OrdinalIgnoreCase) ||
                         visitType.Contains("больниц", StringComparison.OrdinalIgnoreCase))
                    typeId = 2;
            }

            // Преобразование даты в формат YYYY-MM-DD
            string? startDate = !string.IsNullOrWhiteSpace(date) ? ParseDateInput(date) : null;

            var history = await _limaApiClient.GetVisitHistoryAsync(page, typeId, organizationName, startDate);

            if (history.Result.Count == 0)
            {
                return page == 1 ? "📋 История визитов пуста." : $"📋 На странице {page} визитов не найдено.";
            }

            var result = new StringBuilder();
            result.AppendLine($"📋 История визитов (страница {history.Page.PageNumber} из {history.Page.TotalPages}):");
            result.AppendLine($"Всего найдено: {history.Page.Count} записей\n");

            foreach (var visit in history.Result)
            {
                var visitIcon = visit.VisitType == 1 ? "🏪" : "🏥";
                var dateStr = visit.DateCreate.ToString("dd.MM.yyyy HH:mm");
                
                result.AppendLine($"{visitIcon} {visit.Organization.Name}");
                result.AppendLine($"📅 {dateStr} | {visit.VisitStatusName}");
                
                if (visit.Doctor != null)
                {
                    result.AppendLine($"👨‍⚕️ {visit.Doctor.FullName}");
                }

                // Препараты в зависимости от типа визита
                var drugsToShow = visit.OrderStatus == 0 ? visit.TalkedAboutDrugs.Cast<object>().ToList() : 
                                  visit.Drugs.Cast<object>().ToList();
                
                if (drugsToShow.Count > 0)
                {
                    result.Append("💊 ");
                    result.AppendLine(string.Join(", ", drugsToShow.Take(3).Select(d => 
                        d is TalkedAboutDrug td ? td.DrugName : 
                        d is OrderDrug od ? $"{od.Package} уп." : d.ToString())));
                    
                    if (drugsToShow.Count > 3)
                        result.AppendLine($"   ... и ещё {drugsToShow.Count - 3}");
                }

                if (visit.TotalSum > 0)
                {
                    result.AppendLine($"💰 Сумма: {visit.TotalSum:F2} сум");
                }

                result.AppendLine();
            }

            if (history.Page.HasNextPage)
            {
                result.AppendLine($"▶️ Для просмотра следующей страницы скажите: \"Покажи страницу {page + 1}\"");
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при получении истории визитов");
            return $"❌ Произошла ошибка при получении истории: {ex.Message}";
        }
    }

    /// <summary>
    /// Функция №4: Поиск организации по названию
    /// </summary>
    public async Task<string> SearchOrganizationsAsync(string organizationName)
    {
        try
        {
            _logger.Info($"Поиск организаций по запросу: '{organizationName}'");

            var searchResult = await _limaApiClient.SearchOrganizationsAsync(organizationName);

            if (searchResult.Result.Count == 0)
            {
                return $"🔍 По запросу '{organizationName}' организации не найдены.";
            }

            var result = new StringBuilder();
            result.AppendLine($"🔍 Найдено организаций: {searchResult.Result.Count}\n");

            foreach (var org in searchResult.Result.Take(10)) // Показываем максимум 10
            {
                var icon = org.TypeName.Contains("аптека", StringComparison.OrdinalIgnoreCase) ? "🏪" : "🏥";
                
                result.AppendLine($"{icon} {org.Name}");
                result.AppendLine($"📍 {org.Address}");
                result.AppendLine($"🏷️ {org.TypeName} | {org.RegionName}");
                
                if (!string.IsNullOrEmpty(org.Phone))
                    result.AppendLine($"📞 {org.Phone}");
                
                result.AppendLine();
            }

            if (searchResult.Result.Count > 10)
            {
                result.AppendLine($"... и ещё {searchResult.Result.Count - 10} организаций");
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при поиске организаций: '{organizationName}'");
            return $"❌ Произошла ошибка при поиске: {ex.Message}";
        }
    }

    /// <summary>
    /// Функция №5: Просмотр плана визитов
    /// </summary>
    public async Task<string> GetPlannedVisitsAsync(string? date = null, string viewType = "день")
    {
        try
        {
            _logger.Info($"Получение плана визитов: дата={date}, тип={viewType}");

            if (viewType.Contains("месяц"))
            {
                var monthPlan = await _limaApiClient.GetMonthPlansAsync();
                
                if (monthPlan.Count == 0)
                {
                    return "📅 На этот месяц визиты не запланированы.";
                }

                var result = new StringBuilder();
                result.AppendLine("📅 План визитов на месяц:\n");
                
                var totalVisits = monthPlan.Sum(p => p.VisitCount);
                result.AppendLine($"📊 Всего запланировано визитов: {totalVisits}\n");

                foreach (var planItem in monthPlan.OrderBy(p => p.Date))
                {
                    var planDate = DateTime.ParseExact(planItem.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var dayOfWeek = planDate.ToString("dddd", new CultureInfo("ru-RU"));
                    
                    result.AppendLine($"📅 {planDate:dd.MM} ({dayOfWeek}) — {planItem.VisitCount} визитов");
                }

                return result.ToString();
            }
            else
            {
                // Обработка конкретной даты
                string targetDate;
                
                if (string.IsNullOrWhiteSpace(date))
                {
                    targetDate = DateTime.Today.ToString("yyyy-MM-dd");
                }
                else
                {
                    targetDate = ParseDateInput(date);
                }

                var dayPlan = await _limaApiClient.GetPlannedVisitsAsync(targetDate);
                
                if (dayPlan.Result.Count == 0)
                {
                    var displayDate = DateTime.ParseExact(targetDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    return $"📅 На {displayDate:dd.MM.yyyy} визиты не запланированы.";
                }

                var result = new StringBuilder();
                var displayTargetDate = DateTime.ParseExact(targetDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                result.AppendLine($"📅 План визитов на {displayTargetDate:dd.MM.yyyy}:\n");

                foreach (var visit in dayPlan.Result)
                {
                    var icon = visit.Organization.TypeId == 1 ? "🏪" : "🏥";
                    
                    result.AppendLine($"{icon} {visit.Organization.Name}");
                    result.AppendLine($"📍 {visit.Organization.Address}");
                    result.AppendLine($"🕐 {visit.StartDate:HH:mm} | {visit.VisitStatusName}");
                    
                    if (visit.Doctor != null)
                    {
                        result.AppendLine($"👨‍⚕️ {visit.Doctor.FullName} ({visit.Doctor.Position})");
                    }
                    
                    result.AppendLine();
                }

                return result.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при получении плана визитов: дата={date}, тип={viewType}");
            return $"❌ Произошла ошибка при получении плана: {ex.Message}";
        }
    }

    /// <summary>
    /// Получение остатков препарата по названию или всех препаратов
    /// </summary>
    public async Task<string> GetDrugStockAsync(string? drugName = null)
    {
        try
        {
            // Если название не указано, показываем все остатки
            if (string.IsNullOrWhiteSpace(drugName))
            {
                _logger.Info("Получение остатков всех препаратов");
                
                var allDrugs = await _drugSearchService.GetAllDrugBalancesAsync();
                
                if (allDrugs.Count == 0)
                {
                    return "❌ Прайс-лист пуст или недоступен.";
                }

                var result = new StringBuilder();
                result.AppendLine($"📋 Остатки всех препаратов ({allDrugs.Count}):");
                result.AppendLine();

                // Сортируем по названию и показываем первые 20
                var drugsToShow = allDrugs
                    .OrderBy(d => d.Drug.DrugName)
                    .Take(20)
                    .ToList();

                foreach (var drug in drugsToShow)
                {
                    var stockStatus = drug.ActualBalance > 10 ? "✅" : drug.ActualBalance > 0 ? "⚠️" : "❌";
                    result.AppendLine($"{stockStatus} {drug.Drug.DrugName} — {drug.ActualBalance} уп.");
                    
                    if (drug.Price.HasValue)
                        result.Append($" (цена: {drug.Price:F2} сум)");
                    result.AppendLine();
                }

                if (allDrugs.Count > 20)
                {
                    result.AppendLine($"\n... и ещё {allDrugs.Count - 20} препаратов");
                    result.AppendLine("Для поиска конкретного препарата укажите его название.");
                }

                // Общая статистика
                var inStock = allDrugs.Count(d => d.ActualBalance > 0);
                var lowStock = allDrugs.Count(d => d.ActualBalance > 0 && d.ActualBalance <= 10);
                var outOfStock = allDrugs.Count(d => d.ActualBalance == 0);
                
                result.AppendLine($"\n📊 Статистика:");
                result.AppendLine($"✅ В наличии: {inStock}");
                result.AppendLine($"⚠️ Мало: {lowStock} (≤10 уп.)");
                result.AppendLine($"❌ Нет в наличии: {outOfStock}");

                return result.ToString();
            }

            // Поиск конкретного препарата
            _logger.Info($"Получение остатков препарата: '{drugName}'");

            var foundDrug = await _drugSearchService.FindDrugInPriceListAsync(drugName, 60);
            
            if (foundDrug == null)
            {
                // Попробуем найти похожие препараты
                var similarDrugs = await _drugSearchService.SearchSimilarDrugsAsync(drugName, 50, 5);
                
                if (similarDrugs.Count == 0)
                {
                    return $"❌ Препарат '{drugName}' не найден в прайс-листе.";
                }

                var result = new StringBuilder();
                result.AppendLine($"❓ Препарат '{drugName}' не найден. Возможно, вы имели в виду:");
                
                foreach (var drug in similarDrugs)
                {
                    var stockStatus = drug.ActualBalance > 10 ? "✅" : drug.ActualBalance > 0 ? "⚠️" : "❌";
                    result.AppendLine($"{stockStatus} {drug.Drug.DrugName} — остаток: {drug.ActualBalance} уп.");
                }

                return result.ToString();
            }
            else
            {
                var result = new StringBuilder();
                var stockStatus = foundDrug.ActualBalance > 10 ? "✅" : foundDrug.ActualBalance > 0 ? "⚠️" : "❌";
                
                result.AppendLine($"{stockStatus} {foundDrug.Drug.DrugName}");
                result.AppendLine($"📦 Остаток: {foundDrug.ActualBalance} упаковок");
                
                if (foundDrug.Drug.Quantity.HasValue)
                    result.AppendLine($"🔢 В упаковке: {foundDrug.Drug.Quantity} шт.");
                
                if (foundDrug.Price.HasValue)
                    result.AppendLine($"💰 Цена: {foundDrug.Price:F2} сум");

                return result.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при получении остатков препарата: '{drugName}'");
            return $"❌ Произошла ошибка при получении остатков: {ex.Message}";
        }
    }

    /// <summary>
    /// Парсинг даты из различных форматов
    /// </summary>
    private string ParseDateInput(string input)
    {
        var normalizedInput = input.ToLowerInvariant().Trim();
        var today = DateTime.Today;

        // Дни недели
        if (normalizedInput.Contains("понедельник") || normalizedInput.Contains("monday"))
            return GetNextWeekday(today, DayOfWeek.Monday).ToString("yyyy-MM-dd");
        if (normalizedInput.Contains("вторник") || normalizedInput.Contains("tuesday"))
            return GetNextWeekday(today, DayOfWeek.Tuesday).ToString("yyyy-MM-dd");
        if (normalizedInput.Contains("среда") || normalizedInput.Contains("wednesday"))
            return GetNextWeekday(today, DayOfWeek.Wednesday).ToString("yyyy-MM-dd");
        if (normalizedInput.Contains("четверг") || normalizedInput.Contains("thursday"))
            return GetNextWeekday(today, DayOfWeek.Thursday).ToString("yyyy-MM-dd");
        if (normalizedInput.Contains("пятниц") || normalizedInput.Contains("friday"))
            return GetNextWeekday(today, DayOfWeek.Friday).ToString("yyyy-MM-dd");
        if (normalizedInput.Contains("суббот") || normalizedInput.Contains("saturday"))
            return GetNextWeekday(today, DayOfWeek.Saturday).ToString("yyyy-MM-dd");
        if (normalizedInput.Contains("воскресень") || normalizedInput.Contains("sunday"))
            return GetNextWeekday(today, DayOfWeek.Sunday).ToString("yyyy-MM-dd");

        // Относительные даты
        if (normalizedInput.Contains("сегодня") || normalizedInput.Contains("today"))
            return today.ToString("yyyy-MM-dd");
        if (normalizedInput.Contains("завтра") || normalizedInput.Contains("tomorrow"))
            return today.AddDays(1).ToString("yyyy-MM-dd");
        if (normalizedInput.Contains("вчера") || normalizedInput.Contains("yesterday"))
            return today.AddDays(-1).ToString("yyyy-MM-dd");

        // Попытка парсинга как дата
        if (DateTime.TryParse(input, out var parsedDate))
            return parsedDate.ToString("yyyy-MM-dd");

        // По умолчанию - сегодня
        return today.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Получение следующего дня недели
    /// </summary>
    private DateTime GetNextWeekday(DateTime startDate, DayOfWeek targetDay)
    {
        var daysUntilTarget = ((int)targetDay - (int)startDate.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0 && startDate.DayOfWeek == targetDay)
            daysUntilTarget = 7; // Следующая неделя, если сегодня тот же день
        return startDate.AddDays(daysUntilTarget);
    }
}