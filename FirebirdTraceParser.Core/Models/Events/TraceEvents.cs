using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceParser.Core.Models.Events;

/// <summary>
/// Событие начала trace‑сессии.
/// Соответствует Python TraceInitEvent.
/// </summary>
public sealed class TraceInitEvent : EventBase
{
    public required TraceSessionInfo Session { get; init; }
}

/// <summary>
/// Событие завершения trace‑сессии.
/// Соответствует Python TraceFinishEvent.
/// </summary>
public sealed class TraceFinishEvent : EventBase
{
    public required TraceSessionInfo Session { get; init; }
}

/// <summary>
/// Событие подключения к БД.
/// Соответствует Python AttachDatabaseEvent.
/// </summary>
public sealed class AttachDatabaseEvent : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
}

/// <summary>
/// Событие отключения от БД.
/// Соответствует Python DetachDatabaseEvent.
/// </summary>
public sealed class DetachDatabaseEvent : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
}