using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using FirebirdTraceParser.Core.Exceptions;

namespace FirebirdTraceParser.Core.Parsing.Rules;

public interface IRuleLoader
{
    IReadOnlyDictionary<string, Regex> LoadRules(string configPath);
}

public sealed class JsonRuleLoader : IRuleLoader
{
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private const int SupportedSchemaVersion = 1;
    
    // Маппинг флагов (аналог Python FLAG_MAP)
    private static readonly IReadOnlyDictionary<string, RegexOptions> FlagMap = new Dictionary<string, RegexOptions>
    {
        ["IgnoreCase"] = RegexOptions.IgnoreCase,
        ["Multiline"] = RegexOptions.Multiline,
        ["Singleline"] = RegexOptions.Singleline,
        ["IgnorePatternWhitespace"] = RegexOptions.IgnorePatternWhitespace,
        ["ExplicitCapture"] = RegexOptions.ExplicitCapture
    };
    
    public JsonRuleLoader(IMemoryCache cache, ILogger logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public IReadOnlyDictionary<string, Regex> LoadRules(string configPath)
    {
        var fileInfo = new FileInfo(configPath);
        if (!fileInfo.Exists)
            throw new RuleValidationException($"Файл конфигурации не найден: {configPath}");
        
        var cacheKey = $"Rules_{configPath}_{fileInfo.LastWriteTimeUtc.Ticks}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromHours(1));
            
            _logger.Info("Loading rules from: {ConfigPath}", configPath);
            
            // 1. Загрузка JSON
            var json = File.ReadAllText(configPath);
            using var document = JsonDocument.Parse(json);
            
            // 2. Валидация версии схемы
            var schemaVersion = document.RootElement.GetProperty("schemaVersion").GetInt32();
            if (schemaVersion != SupportedSchemaVersion)
                throw new SchemaVersionException(SupportedSchemaVersion, schemaVersion);
            
            // 3. Десериализация
            var config = JsonSerializer.Deserialize<RuleConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
            
            // 4. Компиляция и валидация
            var compiled = CompileAndValidate(config.Rules);
            
            _logger.Info("Loaded {RuleCount} rules successfully", compiled.Count);
            return (IReadOnlyDictionary<string, Regex>)compiled;
        })!;
    }
    
    private Dictionary<string, Regex> CompileAndValidate(Dictionary<string, RuleDefinition> rules)
    {
        var compiled = new Dictionary<string, Regex>(rules.Count);
        
        foreach (var (name, rule) in rules)
        {
            try
            {
                // Парсинг флагов (аналог Python _resolve_flags_cached)
                var options = ParseFlags(rule.Flags) | RegexOptions.Compiled;
                
                // Компиляция regex с таймаутом
                var regex = new Regex(rule.Pattern, options, TimeSpan.FromSeconds(1));
                
                // Валидация обязательных групп
                var specGroups = new HashSet<string>(rule.RequiredGroups);
                var actualGroups = new HashSet<string>(regex.GetGroupNames().Where(g => !int.TryParse(g, out _)));
                var missingGroups = specGroups.Except(actualGroups).ToList();
                
                if (missingGroups.Any())
                {
                    throw new RuleValidationException(
                        $"Правило '{name}' не содержит группу: {string.Join(", ", missingGroups)}",
                        name);
                }
                
                // Валидация sample (STRICT - должно падать как в Python)
                if (!string.IsNullOrEmpty(rule.Sample) && !regex.IsMatch(rule.Sample))
                {
                    throw new RuleValidationException(
                        $"Правило '{name}' не совпадает с sample data",
                        name) 
                    { 
                        SampleData = rule.Sample 
                    };
                }
                
                compiled[name] = regex;
                _logger.Debug("Compiled rule: {RuleName}", name);
            }
            catch (Exception ex) when (ex is not RuleValidationException)
            {
                throw new RuleValidationException($"Ошибка компиляции правила '{name}': {ex.Message}", name);
            }
        }
        
        return compiled;
    }
    
    private static RegexOptions ParseFlags(string[]? flags)
    {
        if (flags == null || flags.Length == 0)
            return RegexOptions.IgnorePatternWhitespace;
        
        var result = RegexOptions.None;
        foreach (var flag in flags)
        {
            if (!FlagMap.TryGetValue(flag, out var option))
                throw new RuleValidationException($"Неизвестный флаг regex: {flag}");
            
            result |= option;
        }
        return result;
    }
}

// Внутренние модели для десериализации
internal sealed class RuleConfiguration
{
    public int SchemaVersion { get; set; }
    public Dictionary<string, RuleDefinition> Rules { get; set; } = new();
}

internal sealed class RuleDefinition
{
    public required string Pattern { get; set; }
    public string[]? Flags { get; set; }
    public string Description { get; set; } = "";
    public string[] RequiredGroups { get; set; } = Array.Empty<string>();
    public string? Sample { get; set; }
}