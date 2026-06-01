using System.Collections.Concurrent;
using FirebirdTraceParser.Core.Models.ValueObjects;

namespace FirebirdTraceParser.Core.Parsing.Utils;

public static class TraceSessionInfoPool
{
    private static readonly ConcurrentDictionary<int, TraceSessionInfo> Pool = new();

    public static TraceSessionInfo Get(int sessionId)
    {
        return Pool.GetOrAdd(sessionId, id => new TraceSessionInfo { SessionId = id });
    }

    public static void Reset()
    {
        Pool.Clear();
    }
}