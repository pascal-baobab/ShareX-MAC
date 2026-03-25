using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ShareXMac.Core.History;

namespace ShareXMac.App.ViewModels;

/// <summary>
/// ViewModel for the HistoryView. Loads capture records from ICaptureHistory,
/// supports search (300ms debounce) and mode filter chips, and exposes
/// delete/copy commands for history items.
/// </summary>
public sealed class HistoryViewModel : ReactiveObject
{
    private readonly ICaptureHistory _history;

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    private string? _selectedModeFilter = "All";
    public string? SelectedModeFilter
    {
        get => _selectedModeFilter;
        set => this.RaiseAndSetIfChanged(ref _selectedModeFilter, value);
    }

    private HistoryItemViewModel? _selectedRecord;
    public HistoryItemViewModel? SelectedRecord
    {
        get => _selectedRecord;
        set => this.RaiseAndSetIfChanged(ref _selectedRecord, value);
    }

    public ObservableCollection<HistoryItemViewModel> Records { get; } = new();

    public ReactiveCommand<int, Unit> DeleteCommand { get; }
    public ReactiveCommand<HistoryItemViewModel, Unit> CopyCommand { get; }

    /// <summary>Filter chip options matching CaptureMode enum values.</summary>
    public string[] ModeFilters { get; } = ["All", "Region", "Window", "FullScreen", "Freeze"];

    public HistoryViewModel(ICaptureHistory history)
    {
        _history = history;

        DeleteCommand = ReactiveCommand.CreateFromTask<int>(OnDeleteAsync);
        CopyCommand = ReactiveCommand.CreateFromTask<HistoryItemViewModel>(OnCopyAsync);

        // Debounced search: reload history 300ms after search text changes
        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => _ = LoadHistoryAsync());

        // Reload on mode filter change (no debounce needed for chip clicks)
        this.WhenAnyValue(x => x.SelectedModeFilter)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => _ = LoadHistoryAsync());
    }

    /// <summary>
    /// Loads history records from SQLite, applies mode filter server-side
    /// and filename search client-side. Populates Records collection.
    /// </summary>
    public async Task LoadHistoryAsync()
    {
        string? modeFilter = SelectedModeFilter is "All" or null
            ? null
            : SelectedModeFilter;

        var records = await _history.QueryAsync(modeFilter: modeFilter);

        // Client-side filename search (sqlite-net QueryAsync filters by mode only)
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? records
            : records.Where(r => r.FilePath?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true)
                     .ToList();

        Records.Clear();
        foreach (var r in filtered)
            Records.Add(new HistoryItemViewModel(r));
    }

    private async Task OnDeleteAsync(int id)
    {
        await _history.DeleteAsync(id);
        var toRemove = Records.FirstOrDefault(r => r.Id == id);
        if (toRemove != null) Records.Remove(toRemove);
    }

    private Task OnCopyAsync(HistoryItemViewModel item)
    {
        // Copy file to clipboard -- will be wired via IOutputService in 02-05 integration plan.
        return Task.CompletedTask;
    }
}
