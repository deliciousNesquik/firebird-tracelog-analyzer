using System.Text.RegularExpressions;
using FirebirdTraceParser.Parsing.Engine;
using FirebirdTraceParser.Parsing.Handlers;
using FirebirdTraceParser.Parsing.Rules;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace FirebirdTraceParser.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет все службы парсера Firebird Trace Log.
    /// </summary>
    /// <param name="services">Коллекция служб.</param>
    /// <param name="rulesPath">Путь к файлу правил для парсера.</param>
    /// <param name="nlogConfigPath">Путь к конфигурации NLog (опционально).</param>
    public static IServiceCollection AddFirebirdTraceParser(
        this IServiceCollection services,
        string rulesPath,
        string? nlogConfigPath = null)
    {
        // настраиваем логгер, если путь не передан ищем в текущей директории
        var configPath = nlogConfigPath ?? "nlog.config";
        
        if (File.Exists(configPath))
            LogManager.Setup().LoadConfigurationFromFile(configPath);

        // регистрируем логгер для дальнейшего логирования процессов
        services.AddSingleton<ILogger>(provider =>
            LogManager.GetLogger("FirebirdTraceParser"));

        // регистрируем стандартное кеширование
        services.AddMemoryCache();
        
        // сервис загрузки правил
        services.AddSingleton<IRuleLoader, JsonRuleLoader>();

        // лениво загружаем правила
        services.AddSingleton<IReadOnlyDictionary<string, Regex>>(provider =>
        {
            var loader = provider.GetRequiredService<IRuleLoader>();
            return loader.LoadRules(rulesPath);
        });

        // регистрируем стандартный обработчик событий
        services.AddSingleton<IEventHandler, DefaultEventHandler>();

        // регистрируем парсер (Transient - для параллельного использования)
        services.AddTransient<ITraceLogParser, TraceLogParser>();

        return services;
    }

    /// <summary>
    /// Добавляет парсер с кастомными опциями.
    /// </summary>
    public static IServiceCollection AddFirebirdTraceParser(
        this IServiceCollection services,
        string rulesPath,
        Action<ParseOptions> configureOptions,
        string? nlogConfigPath = null)
    {
        services.AddFirebirdTraceParser(rulesPath, nlogConfigPath);

        // Регистрация опций через конфигурацию
        services.Configure(configureOptions);

        return services;
    }
}