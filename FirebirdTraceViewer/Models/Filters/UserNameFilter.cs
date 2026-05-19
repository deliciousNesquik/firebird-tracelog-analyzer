using FirebirdTraceParser.Core.Models.Events;
using FirebirdTraceViewer.Interfaces;

namespace FirebirdTraceViewer.Models.Filters;

public class UserNameFilter : IFilter
{
    private HashSet<string> _selectedUsers = [];

    public string Name => "Пользователь";

    public bool Matches(EventBase @event)
    {
        if (_selectedUsers.Count == 0)
            return true;

        return @event switch
        {
            AttachDatabaseEvent att => _selectedUsers.Contains(att.Attachment.User),
            StatementStartEvent stmt => _selectedUsers.Contains(stmt.Attachment.User),
            DetachDatabaseEvent dett => _selectedUsers.Contains(dett.Attachment.User),
            StatementFinishEvent stmt => _selectedUsers.Contains(stmt.Attachment.User),
            ProcedureStartEvent proc => _selectedUsers.Contains(proc.Attachment.User),
            ProcedureFinishEvent proc => _selectedUsers.Contains(proc.Attachment.User),
            TriggerStartEvent trigger => _selectedUsers.Contains(trigger.Attachment.User),
            TriggerFinishEvent trigger => _selectedUsers.Contains(trigger.Attachment.User),
            _ => false
        };
    }

    public void SelectUser(string userName) => _selectedUsers.Add(userName);
    public void DeselectUser(string userName) => _selectedUsers.Remove(userName);
    public void Reset() => _selectedUsers.Clear();
}