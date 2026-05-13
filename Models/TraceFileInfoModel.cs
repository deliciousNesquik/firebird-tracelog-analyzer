using System;

namespace FTV.Models;

public class TraceFileInfoModel(
    string fileName,
    string filePath,
    long fileSize,
    DateTime startTrace,
    DateTime endTrace,
    long eventCount,
    string fileHash)
{
    public string FileName { get; set; } = fileName;
    public string FilePath { get; set; } = filePath;
    public long FileSize { get; set; } = fileSize;
    public DateTime StartTrace { get; set; } = startTrace;
    public DateTime EndTrace { get; set; } = endTrace;
    public long EventCount { get; set; } = eventCount;

    ///SHA-256 хеш содержимого файла для проверки дубликатов
    public string FileHash { get; set; } = fileHash;
}
