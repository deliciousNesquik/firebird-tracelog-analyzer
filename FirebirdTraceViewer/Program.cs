using System;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia;
using FirebirdTraceParser.Core.Infrastructure.DependencyInjection;
using FirebirdTraceViewer.Interfaces;
using FirebirdTraceViewer.Models;
using FirebirdTraceViewer.Services;
using FirebirdTraceViewer.Services.Filtering;
using FirebirdTraceViewer.Services.Sorting;
using FirebirdTraceViewer.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace FirebirdTraceViewer;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var logger = LogManager.Setup()
            .LoadConfigurationFromFile("NLog.config")
            .GetCurrentClassLogger();

        try
        {
            logger.Debug("Инициализация приложения FirebirdTraceViewer");

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

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static IServiceProvider ConfigureServices()
    {
        var logger = LogManager.GetCurrentClassLogger();
        
        // 1. Создаём конфигурацию
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // 2. Регистрируем сервисы
        var services = new ServiceCollection();

        // Регистрируем IConfiguration
        services.AddSingleton<IConfiguration>(configuration);

        // Strongly-typed Options
        services.Configure<AppSettings>(config:configuration.GetSection("Settings"));
        services.Configure<UiSectionSettings>(config:configuration.GetSection("UI:Sections"));

        // Парсер Firebird
        services.AddFirebirdTraceParser(
            rulesPath: "Configuration/rules.json",
            nlogConfigPath: "Configuration/nlog.config"
        );

        // UI сервисы
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<ISortingService, SortingService>();
        services.AddSingleton<IFilteringService, FilteringService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();

        // 3. Строим провайдер
        var serviceProvider = services.BuildServiceProvider();

        // 4. Валидация при старте
        ValidateParserConfiguration(serviceProvider, logger);

        return serviceProvider;
    }

    private static void ValidateParserConfiguration(IServiceProvider provider, ILogger logger)
    {
        try
        {
            var rules = provider.GetRequiredService<IReadOnlyDictionary<string, Regex>>();
            logger.Info("Правила парсера успешно загружены: {RuleCount} правил", rules.Count);

            foreach (var rule in rules)
            {
                var pattern = rule.Value.ToString();
                var preview = pattern.Length > 50 ? pattern[..50] : pattern;
                logger.Debug("Rule loaded: {RuleName} -> {Pattern}", rule.Key, preview);
            }
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Не удалось загрузить правила парсера. Приложение будет закрыто.");
            throw;
        }
    }
}