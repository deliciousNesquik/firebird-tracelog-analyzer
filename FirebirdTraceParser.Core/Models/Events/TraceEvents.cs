using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Событие начала trace‑сессии.
/// Соответствует Python TraceInitEvent.
/// </summary>
public sealed record TraceInitEvent : EventBase
{
    public required TraceSessionInfo Session { get; init; }
}

/// <summary>
/// Событие завершения trace‑сессии.
/// Соответствует Python TraceFinishEvent.
/// </summary>
public sealed record TraceFinishEvent : EventBase
{
    public required TraceSessionInfo Session { get; init; }
}

/// <summary>
/// Событие подключения к БД.
/// Соответствует Python AttachDatabaseEvent.
/// </summary>
public sealed record AttachDatabaseEvent : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
}

/// <summary>
/// Событие отключения от БД.
/// Соответствует Python DetachDatabaseEvent.
/// </summary>
public sealed record DetachDatabaseEvent : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
}