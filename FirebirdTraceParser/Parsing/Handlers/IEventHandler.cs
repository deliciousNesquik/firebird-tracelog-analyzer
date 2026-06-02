namespace FirebirdTraceParser.Parsing.Handlers;

using System.Text.RegularExpressions;
using FirebirdTraceParser.Models.Events;

/// <summary>
/// Обработчик блока события.
/// </summary>
public interface IEventHandler
{
    EventBase? Handle(
        Match blockHeader,
        IReadOnlyList<string> bodyLines,
        IReadOnlyDictionary<string, Regex> rules);
}