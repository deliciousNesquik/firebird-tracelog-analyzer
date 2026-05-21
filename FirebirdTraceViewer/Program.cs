using System;
using System.Text.RegularExpressions;
using Avalonia;
using FirebirdTraceParser.Core.Infrastructure.DependencyInjection;
using FirebirdTraceViewer.Interfaces;
using FirebirdTraceViewer.Services;
using FirebirdTraceViewer.Services.Sorting;
using FirebirdTraceViewer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace FirebirdTraceViewer;

internal sealed class Program
{
    public static IServiceProvider? Services { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var logger = LogManager.Setup()
            .LoadConfigurationFromFile("NLog.config")
            .GetCurrentClassLogger();

        try
        {
            logger.Debug("Инициализация приложения FirebirdTraceViewer");

            // Создаем DI контейнер
            var services = new ServiceCollection();

            // Конфигурация сервисов
            ConfigureServices(services);

            // Строим провайдер сервисов
            Services = services.BuildServiceProvider();

            // Валидация загрузки правил парсера при старте (fail-fast)
            ValidateParserConfiguration(Services, logger);

            // Запуск Avalonia
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Критическая ошибка при запуске приложения");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    /// <summary>
    /// Конфигурация DI сервисов.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // ========== Библиотека парсера ==========
        services.AddFirebirdTraceParser(
            rulesPath: "Configuration/rules.json",
            nlogConfigPath: "Configuration/nlog.config"
        );

        // ========== Avalonia сервисы ==========
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<ISortingService, SortingService>();

        // ========== ViewModels ==========
        services.AddTransient<MainWindowViewModel>();

        // Дополнительные ViewModels, если потребуются
        // services.AddTransient<SettingsViewModel>();
    }

    /// <summary>
    /// Валидация конфигурации парсера при старте (fail-fast).
    /// </summary>
    private static void ValidateParserConfiguration(IServiceProvider provider, ILogger logger)
    {
        try
        {
            var rules = provider.GetRequiredService<IReadOnlyDictionary<string, Regex>>();
            logger.Info("Правила парсера успешно загружены: {RuleCount} правил", rules.Count);
            
            foreach (var rule in rules)
            {
                logger.Debug("Rule loaded: {RuleName} -> {Pattern}", rule.Key, rule.Value.ToString().Substring(0, Math.Min(50, rule.Value.ToString().Length)));
            }
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Не удалось загрузить правила парсера. Приложение будет закрыто.");
            throw; // Приложение не должно запускаться с невалидными правилами
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}