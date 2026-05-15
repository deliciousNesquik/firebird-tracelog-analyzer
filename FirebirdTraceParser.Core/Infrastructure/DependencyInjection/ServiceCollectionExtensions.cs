using System.Text.RegularExpressions;
using FirebirdTraceParser.Core.Parsing.Engine;
using FirebirdTraceParser.Core.Parsing.Handlers;
using FirebirdTraceParser.Core.Parsing.Rules;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace FirebirdTraceParser.Core.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет все службы парсера Firebird Trace Log.
    /// </summary>
    /// <param name="services">Коллекция служб.</param>
    /// <param name="rulesPath">Путь к файлу правил JSON.</param>
    /// <param name="nlogConfigPath">Путь к конфигурации NLog (опционально).</param>
    public static IServiceCollection AddFirebirdTraceParser(
        this IServiceCollection services,
        string rulesPath,
        string? nlogConfigPath = null)
    {
        // 1. Настройка NLog (если путь не указан, ищем в текущей директории)
        var configPath = nlogConfigPath ?? "nlog.config";
        if (File.Exists(configPath))
        {
            LogManager.Setup().LoadConfigurationFromFile(configPath);
        }

        // 2. Регистрация NLog.ILogger для библиотеки парсера
        services.AddSingleton<NLog.ILogger>(provider =>
            LogManager.GetLogger("FirebirdTraceParser"));

        // 3. Кэширование
        services.AddMemoryCache();

        // 4. Rule Loader (Singleton)
        services.AddSingleton<IRuleLoader, JsonRuleLoader>();

        // 5. Загрузка правил (Singleton с ленивой инициализацией)
        services.AddSingleton<IReadOnlyDictionary<string, Regex>>(provider =>
        {
            var loader = provider.GetRequiredService<IRuleLoader>();
            return loader.LoadRules(rulesPath);
        });

        // 6. Event Handler (Singleton - stateless)
        services.AddSingleton<IEventHandler, DefaultEventHandler>();

        // 7. Парсер (Transient - для параллельного использования)
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