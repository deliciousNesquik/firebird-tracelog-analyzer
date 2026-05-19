using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FirebirdTraceParser.Core.Models.Enums;
using FirebirdTraceViewer.Models.Filters;

namespace FirebirdTraceViewer.ViewModels;

/// <summary>
///     ViewModel для UI фильтра типов событий.
///     Отражает состояние EventTypeFilter в интерфейсе.
/// </summary>
public partial class EventTypeFilterViewModel : ViewModelBase
{
    private readonly EventTypeFilter _filter;
    private readonly Action _onFilterChanged;

    /// <summary>Доступные типы событий с информацией о выборе</summary>
    public ObservableCollection<EventTypeCheckBoxModel> EventTypeCheckBoxes { get; }

    public EventTypeFilterViewModel(EventTypeFilter filter, Action onFilterChanged)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        _onFilterChanged = onFilterChanged ?? throw new ArgumentNullException(nameof(onFilterChanged));

        // Инициализируем список типов для UI
        EventTypeCheckBoxes = new ObservableCollection<EventTypeCheckBoxModel>(
            EventTypeFilter.AvailableTypes.Select(et =>
                new EventTypeCheckBoxModel(et, _filter.IsSelected(et), OnTypeSelectionChanged)
            )
        );
    }

    private void OnTypeSelectionChanged()
    {
        // Синхронизируем состояние модели фильтра с UI
        _filter.Reset();

        foreach (var checkBox in EventTypeCheckBoxes.Where(cb => cb.IsSelected))
            _filter.SelectType(checkBox.EventType);

        // Уведомляем об изменении
        _onFilterChanged();
    }

    [RelayCommand]
    private void SelectAllEventTypes()
    {
        _filter.SelectAll();
        foreach (var checkBox in EventTypeCheckBoxes)
            checkBox.IsSelected = true;

        _onFilterChanged();
    }

    [RelayCommand]
    private void DeselectAllEventTypes()
    {
        _filter.Reset();
        foreach (var checkBox in EventTypeCheckBoxes)
            checkBox.IsSelected = false;

        _onFilterChanged();
    }
}

/// <summary>
///     Модель для CheckBox в UI.
///     Хранит состояние выбора одного типа события.
/// </summary>
public partial class EventTypeCheckBoxModel : ViewModelBase
{
    private readonly Action _onChanged;

    public EventType EventType { get; }

    [ObservableProperty] public partial bool IsSelected { get; set; }

    public string DisplayName => GetEventTypeDisplayName(EventType);

    public EventTypeCheckBoxModel(EventType eventType, bool isSelected, Action onChanged)
    {
        EventType = eventType;
        IsSelected = isSelected;
        _onChanged = onChanged ?? (() => { });
    }

    partial void OnIsSelectedChanged(bool value)
    {
        _onChanged();
    }

    /// <summary>Получает красивое отображение типа события</summary>
    private static string GetEventTypeDisplayName(EventType eventType)
    {
        return eventType switch
        {
            EventType.TraceInit => "TRACE INIT",
            EventType.TraceFinish => "TRACE FINISH",
            EventType.AttachDatabase => "ATTACH DATABASE",
            EventType.DetachDatabase => "DETACH DATABASE",
            EventType.ExecuteStatementStart => "EXECUTE STATEMENT START",
            EventType.ExecuteStatementFinish => "EXECUTE STATEMENT FINISH",
            EventType.ExecuteProcedureStart => "EXECUTE PROCEDURE START",
            EventType.ExecuteProcedureFinish => "EXECUTE PROCEDURE FINISH",
            EventType.ExecuteTriggerStart => "EXECUTE TRIGGER START",
            EventType.ExecuteTriggerFinish => "EXECUTE TRIGGER FINISH",
            EventType.FailedExecuteStatementFinish => "ERROR STATEMENT FINISH",
            EventType.FailedExecuteProcedureFinish => "ERROR PROCEDURE FINISH",
            EventType.FailedExecuteTriggerFinish => "ERROR TRIGGER FINISH",
            EventType.ErrorAtJr => "ERROR AT JResult",
            EventType.ErrorAtJs => "ERROR AT JSet",
            _ => eventType.ToString()
        };
    }
}