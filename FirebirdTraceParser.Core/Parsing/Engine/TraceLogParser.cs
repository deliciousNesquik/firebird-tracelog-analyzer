using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using NLog;
using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceParser.Core.Models.Results;
using FirebirdTraceParser.Core.Parsing.Handlers;

namespace FirebirdTraceParser.Core.Parsing.Engine;

public interface ITraceLogParser
{
    ParsingResult<EventBase> ParseFile(string filePath, ParseOptions? options = null);
    
    Task<ParsingResult<EventBase>> ParseFileAsync(
        string filePath, 
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        ParseOptions? options = null);
    
    IAsyncEnumerable<EventBase> ParseStreamAsync(
        Stream stream,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        ParseOptions? options = null);
}

public sealed class TraceLogParser : ITraceLogParser
{
    private readonly IReadOnlyDictionary<string, Regex> _rules;
    private readonly IEventHandler _handler;
    private readonly ILogger _logger;
    
    public TraceLogParser(
        IReadOnlyDictionary<string, Regex> rules,
        IEventHandler handler,
        ILogger logger)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public ParsingResult<EventBase> ParseFile(string filePath, ParseOptions? options = null)
    {
        options ??= ParseOptions.Default;
        
        _logger.Info("Parsing file: {FilePath}", filePath);
        
        var events = new List<EventBase>();
        var warnings = new List<ParsingWarning>();
        
        using var reader = new StreamReader(filePath, options.Encoding);
        
        string? line;
        int lineNumber = 0;
        var currentBlock = new BlockBuffer();
        
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            ProcessLine(line, lineNumber, currentBlock, events, warnings, options);
        }
        
        // Последний блок
        if (currentBlock.HasData)
            FlushBlock(currentBlock, events, warnings, options);
        
        _logger.Info("Parsing completed: {EventCount} events, {WarningCount} warnings", 
            events.Count, warnings.Count);
        
        return new ParsingResult<EventBase>
        {
            Events = events,
            Warnings = warnings
        };
    }
    
    public async Task<ParsingResult<EventBase>> ParseFileAsync(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        ParseOptions? options = null)
    {
        options ??= ParseOptions.Default;
        
        var fileInfo = new FileInfo(filePath);
        var fileSize = fileInfo.Length;
        long bytesRead = 0;
        
        var events = new List<EventBase>();
        var warnings = new List<ParsingWarning>();
        
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 
            bufferSize: 81920, useAsync: true);
        using var reader = new StreamReader(stream, options.Encoding);
        
        string? line;
        int lineNumber = 0;
        var currentBlock = new BlockBuffer();
        
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineNumber++;
            bytesRead += options.Encoding.GetByteCount(line) + 2;
            
            cancellationToken.ThrowIfCancellationRequested();
            
            ProcessLine(line, lineNumber, currentBlock, events, warnings, options);
            
            if (progress != null && lineNumber % 1000 == 0)
                progress.Report((double)bytesRead / fileSize);
        }
        
        if (currentBlock.HasData)
            FlushBlock(currentBlock, events, warnings, options);
        
        progress?.Report(1.0);
        
        return new ParsingResult<EventBase>
        {
            Events = events,
            Warnings = warnings
        };
    }
    
    public async IAsyncEnumerable<EventBase> ParseStreamAsync(
        Stream stream,
        IProgress<double>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        ParseOptions? options = null)
    {
        options ??= ParseOptions.Default;
        
        using var reader = new StreamReader(stream, options.Encoding);
        
        var buffer = new List<EventBase>(options.BatchSize);
        var currentBlock = new BlockBuffer();
        var warnings = new List<ParsingWarning>();
        
        string? line;
        int lineNumber = 0;
        
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineNumber++;
            cancellationToken.ThrowIfCancellationRequested();
            
            ProcessLine(line, lineNumber, currentBlock, buffer, warnings, options);
            
            // Yield батчами
            if (buffer.Count >= options.BatchSize)
            {
                foreach (var evt in buffer)
                    yield return evt;
                buffer.Clear();
            }
        }
        
        // Последний блок
        if (currentBlock.HasData)
            FlushBlock(currentBlock, buffer, warnings, options);
        
        // Остаток батча
        foreach (var evt in buffer)
            yield return evt;
    }
    
    private void ProcessLine(
        string line,
        int lineNumber,
        BlockBuffer currentBlock,
        List<EventBase> events,
        List<ParsingWarning> warnings,
        ParseOptions options)
    {
        var blockMatch = _rules["block_header"].Match(line);
        
        if (blockMatch.Success)
        {
            // Новый блок - обработать предыдущий
            if (currentBlock.HasData)
            {
                FlushBlock(currentBlock, events, warnings, options);
            }
            
            currentBlock.Reset();
            currentBlock.Header = blockMatch;
            currentBlock.StartLine = lineNumber;
        }
        else if (currentBlock.HasData && !string.IsNullOrWhiteSpace(line))
        {
            currentBlock.BodyLines.Add(line);
        }
    }
    
    private void FlushBlock(
        BlockBuffer block,
        List<EventBase> events,
        List<ParsingWarning> warnings,
        ParseOptions options)
    {
        try
        {
            var evt = _handler.Handle(block.Header!, block.BodyLines, _rules);
            
            if (evt != null)
            {
                events.Add(evt);
            }
            else
            {
                warnings.Add(new ParsingWarning
                {
                    Severity = WarningSeverity.Info,
                    Message = "Block handler returned null",
                    LineNumber = block.StartLine
                });
            }
        }
        catch (Exception ex)
        {
            var severity = options.ValidationMode == ValidationMode.Strict 
                ? WarningSeverity.Warning 
                : WarningSeverity.Error;
            
            warnings.Add(new ParsingWarning
            {
                Severity = severity,
                Message = $"Failed to parse block: {ex.Message}",
                LineNumber = block.StartLine,
                BlockContent = string.Join("\n", block.BodyLines.Take(3))
            });
            
            _logger.Warn(ex, "Failed to parse block at line {LineNumber}", block.StartLine);
        }
    }
    
    private sealed class BlockBuffer
    {
        public Match? Header { get; set; }
        public List<string> BodyLines { get; } = new();
        public int StartLine { get; set; }
        public bool HasData => Header != null;
        
        public void Reset()
        {
            Header = null;
            BodyLines.Clear();
            StartLine = 0;
        }
    }
}