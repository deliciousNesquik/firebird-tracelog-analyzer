using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FirebirdTraceViewer.Core;

public sealed class RangeObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
            return;

        CheckReentrancy();

        var startIndex = Count;

        var addedItems = items.ToList();

        foreach (var item in addedItems)
            Items.Add(item);

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                addedItems,
                startIndex));

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
    }
    
    public void ReplaceRange(IEnumerable<T> items)
    {
        if (items == null)
            return;

        CheckReentrancy();

        var newItems = items.ToList();

        Items.Clear();

        foreach (var item in newItems)
            Items.Add(item);

        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
    }
}