using System;

namespace FTV.Models;

public class TraceFileInfoModel(string fileName, long fileSize, DateTime startTrace, DateTime endTrace, long eventCount)
{
    public string FileName { get; set; } = fileName;
    public long FileSize { get; set; } = fileSize;
    public DateTime StartTrace { get; set; } = startTrace;
    public DateTime EndTrace { get; set; } = endTrace;
    public long EventCount { get; set; } = eventCount;
}