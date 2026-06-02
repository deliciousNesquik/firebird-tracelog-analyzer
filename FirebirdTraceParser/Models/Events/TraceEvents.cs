using FirebirdTraceParser.Models.ValueObjects;

namespace FirebirdTraceParser.Models.Events;

/// <summary>
/// Событие начала trace‑сессии.
/// </summary>
public sealed class TraceInitEvent : EventBase
{
    public required TraceSessionInfo Session { get; init; }
}

/// <summary>
/// Событие завершения trace‑сессии.
/// </summary>
public sealed class TraceFinishEvent : EventBase
{
    public required TraceSessionInfo Session { get; init; }
}

/// <summary>
/// Событие подключения к БД.
/// </summary>
public sealed class AttachDatabaseEvent : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
}

/// <summary>
/// Событие отключения от БД.
/// </summary>
public sealed class DetachDatabaseEvent : EventBase
{
    public required AttachmentInfo Attachment { get; init; }
}