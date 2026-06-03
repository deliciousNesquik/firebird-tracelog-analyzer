using FirebirdTraceAnalyzer.Enums.Reports;
using FirebirdTraceAnalyzer.Models.Reports;
using FirebirdTraceAnalyzer.Services.Reports.Exporters;
using FirebirdTraceParser.Models.Events;
using NLog;

namespace FirebirdTraceAnalyzer.Services.Reports;

/// <summary>
///     Реализация сервиса генерации отчётов
/// </summary>
public class ReportGenerationService : IReportGenerationService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<ReportFormat, IReportExporter> _exporters;
    private readonly string _reportsDirectory;

    public ReportGenerationService(
        PdfReportExporter pdfExporter,
        DocxReportExporter docxExporter,
        XlsxReportExporter xlsxExporter,
        CsvReportExporter csvExporter)
    {
        _exporters = new Dictionary<ReportFormat, IReportExporter>
        {
            [ReportFormat.PDF] = pdfExporter,
            [ReportFormat.DOCX] = docxExporter,
            [ReportFormat.XLSX] = xlsxExporter,
            [ReportFormat.CSV] = csvExporter
        };

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _reportsDirectory = Path.Combine(appDataPath, "FirebirdTraceAnalyzer", "Reports", "History");

        if (!Directory.Exists(_reportsDirectory))
        {
            Directory.CreateDirectory(_reportsDirectory);
            Logger.Info("Created reports directory: {Path}", _reportsDirectory);
        }
    }

    public async Task<GeneratedReport> GenerateReportAsync(
        ReportTemplate template,
        ReportMetadata metadata,
        ReportFormat format,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.Info("Generating report: {TemplateName} ({Format})", template.Name, format);

            // Получаем экспортер
            if (!_exporters.TryGetValue(format, out var exporter))
                throw new NotSupportedException($"Format not supported: {format}");

            // Создаём путь для сохранения, если не указан
            if (string.IsNullOrWhiteSpace(outputPath)) outputPath = GenerateOutputPath(template, format);

            // Генерируем отчёт
            await exporter.ExportAsync(template, metadata, outputPath, cancellationToken);

            // Получаем информацию о файле
            var fileInfo = new FileInfo(outputPath);

            var generatedReport = new GeneratedReport
            {
                Template = template,
                Metadata = metadata,
                Format = format,
                FilePath = outputPath,
                FileSize = fileInfo.Length,
                GeneratedAt = DateTime.Now
            };

            Logger.Info("Report generated successfully: {Path} ({Size} bytes)",
                outputPath, fileInfo.Length);

            return generatedReport;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error generating report: {TemplateName}", template.Name);
            throw;
        }
    }

    public IReadOnlyList<EventBase> PrepareEventsForReport(
        IEnumerable<EventBase> visibleEvents,
        ReportTemplate template,
        string? currentSortField,
        bool currentSortDescending)
    {
        var events = visibleEvents.ToList();

        Logger.Info("Preparing events for report: {TemplateName}", template.Name);
        Logger.Debug("Input events count: {Count}", events.Count);

        // ✅ ШАГ 1: Применяем фильтр по типам событий (если указан в шаблоне)
        if (template.EventTypeFilter != null && template.EventTypeFilter.Count > 0)
        {
            var allowedTypes = new HashSet<string>(template.EventTypeFilter);

            events = events
                .Where(e => allowedTypes.Contains(e.GetType().Name))
                .ToList();

            Logger.Debug("After event type filter: {Count} events (filtered by: {Types})",
                events.Count, string.Join(", ", template.EventTypeFilter));
        }

        // ✅ ШАГ 2: Проверяем и применяем сортировку (если отличается от текущей)
        if (!string.IsNullOrWhiteSpace(template.SortByField))
        {
            // Проверяем, совпадает ли текущая сортировка с требуемой
            var needsResorting = currentSortField != template.SortByField ||
                                 currentSortDescending != template.SortDescending;

            if (needsResorting)
            {
                events = SortEvents(events, template.SortByField, template.SortDescending);

                Logger.Debug("Applied sorting: {Field} ({Direction})",
                    template.SortByField,
                    template.SortDescending ? "DESC" : "ASC");
            }
            else
            {
                Logger.Debug("Sorting already matches template, skipping");
            }
        }

        // ✅ ШАГ 3: Применяем лимит (если указан)
        if (template.EventLimit.HasValue && template.EventLimit.Value > 0)
        {
            events = events.Take(template.EventLimit.Value).ToList();

            Logger.Debug("Applied limit: {Limit} events", template.EventLimit.Value);
        }

        Logger.Info("Events prepared: {Count} events ready for report", events.Count);

        return events;
    }

    /// <summary>
    ///     Сортирует события по указанному полю
    /// </summary>
    private List<EventBase> SortEvents(List<EventBase> events, string fieldPath, bool descending)
    {
        var sorted = events.OrderBy(e =>
        {
            var value = GetPropertyValue(e, fieldPath);
            return value;
        }, Comparer<object?>.Create((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            if (a is IComparable comparableA && b is IComparable comparableB) return comparableA.CompareTo(comparableB);

            return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
        }));

        return descending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    /// <summary>
    ///     Получает значение свойства по пути (например, "Performance.ExecuteMs")
    /// </summary>
    private object? GetPropertyValue(object obj, string propertyPath)
    {
        var parts = propertyPath.Split('.');
        var current = obj;

        foreach (var part in parts)
        {
            if (current == null) return null;

            var prop = current.GetType().GetProperty(part);
            if (prop == null) return null;

            current = prop.GetValue(current);
        }

        return current;
    }

    /// <summary>
    ///     Генерирует путь для сохранения отчёта
    /// </summary>
    private string GenerateOutputPath(ReportTemplate template, ReportFormat format)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var sanitizedName = SanitizeFileName(template.Name);
        var extension = GetFileExtension(format);

        var fileName = $"{timestamp}_{sanitizedName}{extension}";

        return Path.Combine(_reportsDirectory, fileName);
    }

    private static string GetFileExtension(ReportFormat format)
    {
        return format switch
        {
            ReportFormat.PDF => ".pdf",
            ReportFormat.DOCX => ".docx",
            ReportFormat.XLSX => ".xlsx",
            ReportFormat.CSV => ".csv",
            _ => ".txt"
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}