using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;

namespace RobustEstimation.ViewModels.Methods;

public partial class TheilSenMethodViewModel : ViewModelBase
{
    private readonly Dataset _dataset;
    private readonly MainWindowViewModel _mainViewModel;
    private CancellationTokenSource _cts;

    [ObservableProperty]
    private string result = "Not computed";

    [ObservableProperty]
    private string processedSlopes = "";

    [ObservableProperty]
    private double progress;

    public TheilSenMethodViewModel(Dataset dataset, MainWindowViewModel mainViewModel)
    {
        _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        ComputeCommand = new AsyncRelayCommand(ComputeTheilSenAsync, () => _dataset.Values.Any());
        _dataset.PropertyChanged += (_, _) => ComputeCommand.NotifyCanExecuteChanged();
    }

    public IAsyncRelayCommand ComputeCommand { get; }

    private async Task ComputeTheilSenAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        Result = "Calculating...";
        ProcessedSlopes = "";
        Progress = 0;
        _mainViewModel.Progress = 0;

        var progress = new Progress<int>(p =>
        {
            Progress = p;
            _mainViewModel.Progress = p;
        });

        try
        {
            var estimator = new TheilSenEstimator();
            var result = await estimator.ComputeWithTimingAsync(_dataset, progress, _cts.Token);
            
            string processedData = $"[{string.Join(", ", estimator.ProcessedSlopes.Take(100))}]";

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Result = $"Result: {result.result:F2} (Time: {result.duration.TotalMilliseconds} ms)";
                ProcessedSlopes = $"Computed slopes: {processedData}";
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() => Result = "Calculation canceled.");
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => Result = $"Error: {ex.Message}");
        }
    }
}
