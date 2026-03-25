using ReactiveUI;
using ShareXMac.Core.History;

namespace ShareXMac.App.ViewModels;

public sealed class MainWindowViewModel : ReactiveObject
{
    public HistoryViewModel History { get; }

    public MainWindowViewModel(ICaptureHistory captureHistory)
    {
        History = new HistoryViewModel(captureHistory);
    }
}
