using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceParser.Core.Models.Results;

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