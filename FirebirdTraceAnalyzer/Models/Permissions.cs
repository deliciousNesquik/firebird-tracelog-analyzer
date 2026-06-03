namespace FirebirdTraceAnalyzer.Models;

public sealed class Permissions(bool ownerCanRead, bool ownerCanWrite, bool ownerCanExecute, bool groupCanRead, bool groupCanWrite, bool groupCanExecute, bool othersCanRead, bool othersCanWrite, bool othersCanExecute)
{
    public bool OwnerCanExecute { get; set; } = ownerCanExecute;
    public bool OwnerCanRead { get; set; }  = ownerCanRead;
    public bool OwnerCanWrite { get; set; } = ownerCanWrite;
    
    public bool GroupCanExecute { get; set; } = groupCanExecute;
    public bool GroupCanRead { get; set; } = groupCanRead;
    public bool GroupCanWrite { get; set; } = groupCanWrite;
    
    public bool OthersCanExecute { get; set; } = othersCanExecute;
    public bool OthersCanRead { get; set; } = othersCanRead;
    public bool OthersCanWrite { get; set; } = othersCanWrite;
}