using FirebirdTraceAnalyzer.Enums.Reports;
using FirebirdTraceAnalyzer.Models.Reports;

namespace FirebirdTraceAnalyzer.Services.Reports.BuiltInTemplates;

/// <summary>
/// Встроенные шаблоны отчётов
/// </summary>
public static class BuiltInReportTemplates
{
    public static IReadOnlyList<ReportTemplate> GetAll()
    {
        return new List<ReportTemplate>
        {
            CreateTop5StatementsTemplate(),
            CreateTop10ProceduresTemplate(),
            CreateUserActivityTemplate(),
            CreateErrorReportTemplate()
        };
    }

    /// <summary>
    /// Шаблон "Top 5 Slowest Statements"
    /// </summary>
    private static ReportTemplate CreateTop5StatementsTemplate()
    {
        return new ReportTemplate
        {
            Id = "builtin_top5_statements",
            Name = "Top 5 Slowest Statements",
            Description = "Report showing the 5 slowest SQL statements",
            Author = "System",
            Category = ReportCategory.Quick,
            IsBuiltIn = true,
            
            Header = new ReportHeader
            {
                Title = "Top 5 Slowest SQL Statements",
                Subtitle = "Performance Analysis Report",
                ShowLogo = true,
                ShowGeneratedDate = true,
                Variables = new List<ReportVariable>
                {
                    new()
                    {
                        Type = ReportVariableType.FileNames,
                        DisplayName = "Analyzed Files",
                        TemplateKey = "{FILE_NAMES}",
                        IsVisible = true,
                        DisplayOrder = 1
                    },
                    new()
                    {
                        Type = ReportVariableType.TotalEventsCount,
                        DisplayName = "Total Events",
                        TemplateKey = "{TOTAL_EVENTS}",
                        Format = "N0",
                        IsVisible = true,
                        DisplayOrder = 2
                    },
                    new()
                    {
                        Type = ReportVariableType.FilteredEventsCount,
                        DisplayName = "Filtered Events",
                        TemplateKey = "{FILTERED_EVENTS}",
                        Format = "N0",
                        IsVisible = true,
                        DisplayOrder = 3
                    },
                    new()
                    {
                        Type = ReportVariableType.TraceDuration,
                        DisplayName = "Trace Duration",
                        TemplateKey = "{TRACE_DURATION}",
                        IsVisible = true,
                        DisplayOrder = 4
                    }
                }
            },
            
            Body = new ReportBody
            {
                DisplayStyle = EventDisplayStyle.Table,
                VisibleFields = new List<EventField>
                {
                    new()
                    {
                        Name = "Timestamp",
                        DisplayName = "Time",
                        PropertyPath = "Timestamp",
                        Format = "yyyy-MM-dd HH:mm:ss",
                        WidthPercent = 15,
                        Order = 1,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "User",
                        DisplayName = "User",
                        PropertyPath = "Attachment.User",
                        WidthPercent = 10,
                        Order = 2,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "ExecutionTime",
                        DisplayName = "Execution Time (ms)",
                        PropertyPath = "Performance.ExecuteMs",
                        Format = "N0",
                        WidthPercent = 12,
                        Order = 3,
                        Alignment = TextAlignment.Right
                    },
                    new()
                    {
                        Name = "SqlText",
                        DisplayName = "SQL Query",
                        PropertyPath = "Sql",
                        WidthPercent = 48,
                        Order = 4,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "RecordsFetched",
                        DisplayName = "Records",
                        PropertyPath = "Performance.FetchCount",
                        Format = "N0",
                        WidthPercent = 8,
                        Order = 5,
                        Alignment = TextAlignment.Right
                    },
                    new()
                    {
                        Name = "Database",
                        DisplayName = "Database",
                        PropertyPath = "Attachment.DatabasePath",
                        WidthPercent = 7,
                        Order = 6,
                        Alignment = TextAlignment.Left
                    }
                },
                ShowSummary = true,
                Sections = new List<ReportSection>
                {
                    new()
                    {
                        Title = "Slowest Statements",
                        Description = "Top 5 statements sorted by execution time",
                        ContentType = SectionContentType.Events,
                        ShowTitle = true,
                        Order = 1
                    },
                    new()
                    {
                        Title = "Summary Statistics",
                        ContentType = SectionContentType.Statistics,
                        ShowTitle = true,
                        Order = 2
                    }
                }
            },
            
            Footer = new ReportFooter
            {
                Show = true,
                Text = "Generated by Flytic - Firebird Trace Analyzer",
                ShowPageNumbers = true
            },
            
            EventTypeFilter = new List<string> { "StatementFinishEvent" },
            SortByField = "Performance.ExecuteMs",
            SortDescending = true,
            EventLimit = 5,
            
            SupportedFormats = new List<ReportFormat> { ReportFormat.PDF, ReportFormat.DOCX, ReportFormat.XLSX, ReportFormat.CSV },
            DefaultFormat = ReportFormat.PDF,
            
            Tags = new List<string> { "performance", "sql", "statements", "top5" }
        };
    }

    /// <summary>
    /// Шаблон "Top 10 Slowest Procedures"
    /// </summary>
    private static ReportTemplate CreateTop10ProceduresTemplate()
    {
        return new ReportTemplate
        {
            Id = "builtin_top10_procedures",
            Name = "Top 10 Slowest Procedures",
            Description = "Report showing the 10 slowest stored procedures",
            Author = "System",
            Category = ReportCategory.Quick,
            IsBuiltIn = true,
            
            Header = new ReportHeader
            {
                Title = "Top 10 Slowest Stored Procedures",
                Subtitle = "Performance Analysis Report",
                ShowLogo = true,
                ShowGeneratedDate = true,
                Variables = new List<ReportVariable>
                {
                    new()
                    {
                        Type = ReportVariableType.FileNames,
                        DisplayName = "Analyzed Files",
                        TemplateKey = "{FILE_NAMES}",
                        IsVisible = true,
                        DisplayOrder = 1
                    },
                    new()
                    {
                        Type = ReportVariableType.TotalEventsCount,
                        DisplayName = "Total Events",
                        TemplateKey = "{TOTAL_EVENTS}",
                        Format = "N0",
                        IsVisible = true,
                        DisplayOrder = 2
                    },
                    new()
                    {
                        Type = ReportVariableType.FilteredEventsCount,
                        DisplayName = "Filtered Events",
                        TemplateKey = "{FILTERED_EVENTS}",
                        Format = "N0",
                        IsVisible = true,
                        DisplayOrder = 3
                    }
                }
            },
            
            Body = new ReportBody
            {
                DisplayStyle = EventDisplayStyle.Table,
                VisibleFields = new List<EventField>
                {
                    new()
                    {
                        Name = "Timestamp",
                        DisplayName = "Time",
                        PropertyPath = "Timestamp",
                        Format = "yyyy-MM-dd HH:mm:ss",
                        WidthPercent = 18,
                        Order = 1,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "ProcedureName",
                        DisplayName = "Procedure Name",
                        PropertyPath = "ProcedureName",
                        WidthPercent = 30,
                        Order = 2,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "User",
                        DisplayName = "User",
                        PropertyPath = "Attachment.User",
                        WidthPercent = 12,
                        Order = 3,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "ExecutionTime",
                        DisplayName = "Execution Time (ms)",
                        PropertyPath = "Performance.ExecuteMs",
                        Format = "N0",
                        WidthPercent = 15,
                        Order = 4,
                        Alignment = TextAlignment.Right
                    },
                    new()
                    {
                        Name = "ReadCount",
                        DisplayName = "Reads",
                        PropertyPath = "Performance.ReadCount",
                        Format = "N0",
                        WidthPercent = 10,
                        Order = 5,
                        Alignment = TextAlignment.Right
                    },
                    new()
                    {
                        Name = "WriteCount",
                        DisplayName = "Writes",
                        PropertyPath = "Performance.WriteCount",
                        Format = "N0",
                        WidthPercent = 10,
                        Order = 6,
                        Alignment = TextAlignment.Right
                    }
                },
                ShowSummary = true,
                Sections = new List<ReportSection>
                {
                    new()
                    {
                        Title = "Slowest Procedures",
                        Description = "Top 10 procedures sorted by execution time",
                        ContentType = SectionContentType.Events,
                        ShowTitle = true,
                        Order = 1
                    },
                    new()
                    {
                        Title = "Summary Statistics",
                        ContentType = SectionContentType.Statistics,
                        ShowTitle = true,
                        Order = 2
                    }
                }
            },
            
            Footer = new ReportFooter
            {
                Show = true,
                Text = "Generated by Flytic - Firebird Trace Analyzer",
                ShowPageNumbers = true
            },
            
            EventTypeFilter = new List<string> { "ProcedureFinishEvent" },
            SortByField = "Performance.ExecuteMs",
            SortDescending = true,
            EventLimit = 10,
            
            SupportedFormats = new List<ReportFormat> { ReportFormat.PDF, ReportFormat.DOCX, ReportFormat.XLSX, ReportFormat.CSV },
            DefaultFormat = ReportFormat.PDF,
            
            Tags = new List<string> { "performance", "procedures", "top10" }
        };
    }

    /// <summary>
    /// Шаблон "User Activity Summary"
    /// </summary>
    private static ReportTemplate CreateUserActivityTemplate()
    {
        return new ReportTemplate
        {
            Id = "builtin_user_activity",
            Name = "User Activity Summary",
            Description = "Summary of user database activity",
            Author = "System",
            Category = ReportCategory.Quick,
            IsBuiltIn = true,
            
            Header = new ReportHeader
            {
                Title = "User Activity Summary",
                Subtitle = "Database Usage Analysis",
                ShowLogo = true,
                ShowGeneratedDate = true,
                Variables = new List<ReportVariable>
                {
                    new()
                    {
                        Type = ReportVariableType.FileNames,
                        DisplayName = "Analyzed Files",
                        TemplateKey = "{FILE_NAMES}",
                        IsVisible = true,
                        DisplayOrder = 1
                    },
                    new()
                    {
                        Type = ReportVariableType.TraceDuration,
                        DisplayName = "Analysis Period",
                        TemplateKey = "{TRACE_DURATION}",
                        IsVisible = true,
                        DisplayOrder = 2
                    }
                }
            },
            
            Body = new ReportBody
            {
                DisplayStyle = EventDisplayStyle.Table,
                VisibleFields = new List<EventField>
                {
                    new()
                    {
                        Name = "Timestamp",
                        DisplayName = "Time",
                        PropertyPath = "Timestamp",
                        Format = "yyyy-MM-dd HH:mm:ss",
                        WidthPercent = 20,
                        Order = 1,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "User",
                        DisplayName = "User",
                        PropertyPath = "Attachment.User",
                        WidthPercent = 15,
                        Order = 2,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "EventType",
                        DisplayName = "Event Type",
                        PropertyPath = "EventType",
                        WidthPercent = 20,
                        Order = 3,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "Database",
                        DisplayName = "Database",
                        PropertyPath = "Attachment.DatabasePath",
                        WidthPercent = 25,
                        Order = 4,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "Address",
                        DisplayName = "Address",
                        PropertyPath = "Attachment.Address",
                        WidthPercent = 20,
                        Order = 5,
                        Alignment = TextAlignment.Left
                    }
                },
                ShowSummary = true,
                Sections = new List<ReportSection>
                {
                    new()
                    {
                        Title = "User Activity",
                        Description = "All user events during the trace period",
                        ContentType = SectionContentType.Events,
                        ShowTitle = true,
                        Order = 1
                    }
                }
            },
            
            Footer = new ReportFooter
            {
                Show = true,
                Text = "Generated by Flytic - Firebird Trace Analyzer",
                ShowPageNumbers = true
            },
            
            EventTypeFilter = null, // Все типы
            SortByField = "Timestamp",
            SortDescending = false,
            EventLimit = null, // Без лимита
            
            SupportedFormats = new List<ReportFormat> { ReportFormat.PDF, ReportFormat.DOCX, ReportFormat.XLSX, ReportFormat.CSV },
            DefaultFormat = ReportFormat.PDF,
            
            Tags = new List<string> { "users", "activity", "connections" }
        };
    }

    /// <summary>
    /// Шаблон "Error Report"
    /// </summary>
    private static ReportTemplate CreateErrorReportTemplate()
    {
        return new ReportTemplate
        {
            Id = "builtin_error_report",
            Name = "Error Report",
            Description = "Summary of all errors occurred during trace",
            Author = "System",
            Category = ReportCategory.Quick,
            IsBuiltIn = true,
            
            Header = new ReportHeader
            {
                Title = "Error Report",
                Subtitle = "Database Errors Analysis",
                ShowLogo = true,
                ShowGeneratedDate = true,
                Variables = new List<ReportVariable>
                {
                    new()
                    {
                        Type = ReportVariableType.FileNames,
                        DisplayName = "Analyzed Files",
                        TemplateKey = "{FILE_NAMES}",
                        IsVisible = true,
                        DisplayOrder = 1
                    },
                    new()
                    {
                        Type = ReportVariableType.FilteredEventsCount,
                        DisplayName = "Total Errors",
                        TemplateKey = "{FILTERED_EVENTS}",
                        Format = "N0",
                        IsVisible = true,
                        DisplayOrder = 2
                    }
                }
            },
            
            Body = new ReportBody
            {
                DisplayStyle = EventDisplayStyle.DetailedList,
                VisibleFields = new List<EventField>
                {
                    new()
                    {
                        Name = "Timestamp",
                        DisplayName = "Time",
                        PropertyPath = "Timestamp",
                        Format = "yyyy-MM-dd HH:mm:ss",
                        Order = 1,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "Component",
                        DisplayName = "Component",
                        PropertyPath = "Component",
                        Order = 2,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "User",
                        DisplayName = "User",
                        PropertyPath = "Attachment.User",
                        Order = 3,
                        Alignment = TextAlignment.Left
                    },
                    new()
                    {
                        Name = "Database",
                        DisplayName = "Database",
                        PropertyPath = "Attachment.DatabasePath",
                        Order = 4,
                        Alignment = TextAlignment.Left
                    }
                },
                ShowSummary = true,
                Sections = new List<ReportSection>
                {
                    new()
                    {
                        Title = "Errors",
                        Description = "All errors occurred during the trace period",
                        ContentType = SectionContentType.Events,
                        ShowTitle = true,
                        Order = 1
                    }
                }
            },
            
            Footer = new ReportFooter
            {
                Show = true,
                Text = "Generated by Flytic - Firebird Trace Analyzer",
                ShowPageNumbers = true
            },
            
            EventTypeFilter = new List<string> { "ErrorEvent" },
            SortByField = "Timestamp",
            SortDescending = false,
            EventLimit = null,
            
            SupportedFormats = new List<ReportFormat> { ReportFormat.PDF, ReportFormat.DOCX, ReportFormat.CSV },
            DefaultFormat = ReportFormat.PDF,
            
            Tags = new List<string> { "errors", "troubleshooting" }
        };
    }
}