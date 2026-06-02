using System.Text.RegularExpressions;
using Avalonia;
using FirebirdTraceAnalyzer;
using FirebirdTraceParser.Core.Infrastructure.DependencyInjection;
using FirebirdTraceAnalyzer.Interfaces;
using FirebirdTraceAnalyzer.Models;
using FirebirdTraceAnalyzer.Services;
using FirebirdTraceAnalyzer.Services.Filtering;
using FirebirdTraceAnalyzer.Services.Searching;
using FirebirdTraceAnalyzer.Services.Sorting;
using FirebirdTraceAnalyzer.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace FirebirdTraceAnalyzer;

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
            logger.Info("Initializing the application");

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Fatal error while starting application");
            throw;
        }
        finally
        {
            logger.Info("Shutting down the application");
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
        
        // конфигурация приложения, настройки, расположение и прочее
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // DI контейнер для подключения сервисов
        var services = new ServiceCollection();
        
        // настройки приложения сопоставляются с моделями данных для использования объекта как конфигурации
        services.Configure<AppSettings>(config:configuration.GetSection("Settings"));
        services.Configure<UiSectionSettings>(config:configuration.GetSection("UI:Sections"));
        
        services.AddSingleton<IConfiguration>(configuration);

        // используем встроенный в парсере метод для подключения парсера как сервис
        services.AddFirebirdTraceParser(
            rulesPath: "Configuration/rules.json",
            nlogConfigPath: "Configuration/nlog.config"
        );

        // добавляем сервисы для ui приложения
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IWindowProvider, WindowProvider>();
        services.AddSingleton<ISortingService, SortingService>();
        services.AddSingleton<IFilteringService, FilteringService>();
        services.AddSingleton<ISearchService, SearchService>();

        // добавляем ViewModels главного окна
        services.AddTransient<MainWindowViewModel>();

        // собираем все в провайдера
        var serviceProvider = services.BuildServiceProvider();

        // валидируем парсера, потому что без него ничего не сможем обработать!
        ValidateParserConfiguration(serviceProvider, logger);
        
        return serviceProvider;
    }

    private static void ValidateParserConfiguration(IServiceProvider provider, ILogger logger)
    {
        try
        {
            // получаем правила, которые загрузились в парсер
            var rules = provider.GetRequiredService<IReadOnlyDictionary<string, Regex>>();
            
            
            logger.Info("{RuleCount} rule(s) was loaded", rules.Count);

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