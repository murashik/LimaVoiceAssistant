# Проверка компиляции проекта Lima Voice Assistant
Write-Host "🔧 Проверка компиляции Lima Voice Assistant..." -ForegroundColor Green

try {
    # Переходим в директорию проекта
    Set-Location "C:\Users\Vadim\source\repos\LimaVoiceAssistant"
    
    # Очистка предыдущих сборок
    Write-Host "🧹 Очистка предыдущих сборок..." -ForegroundColor Yellow
    dotnet clean
    
    # Восстановление пакетов
    Write-Host "📦 Восстановление NuGet пакетов..." -ForegroundColor Yellow
    dotnet restore --verbosity minimal
    
    # Компиляция проекта
    Write-Host "🔨 Компиляция проекта..." -ForegroundColor Yellow
    dotnet build --configuration Release --no-restore --verbosity minimal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Проект успешно скомпилирован!" -ForegroundColor Green
        Write-Host ""
        Write-Host "🚀 Для запуска используйте:" -ForegroundColor Cyan
        Write-Host "   dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "🌐 После запуска проект будет доступен по адресам:" -ForegroundColor Cyan
        Write-Host "   • HTTPS: https://localhost:5001" -ForegroundColor White
        Write-Host "   • HTTP:  http://localhost:5000" -ForegroundColor White
        Write-Host "   • Swagger: https://localhost:5001/swagger" -ForegroundColor White
        Write-Host ""
        Write-Host "⚙️  Не забудьте настроить appsettings.json!" -ForegroundColor Yellow
    } else {
        Write-Host "❌ Ошибки компиляции!" -ForegroundColor Red
        Write-Host "💡 Проверьте вывод выше для деталей ошибок" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Ошибка: $_" -ForegroundColor Red
}

Write-Host "🔧 Проверка завершена." -ForegroundColor Green