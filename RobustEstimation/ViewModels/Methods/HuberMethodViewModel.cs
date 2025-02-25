using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;

namespace RobustEstimation.ViewModels.Methods;

public partial class HuberMethodViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private CancellationTokenSource _cts;
    private readonly Dataset _dataset;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private double tuningConstant = 1.345;

    [ObservableProperty]
    private string result = "Not computed";

    [ObservableProperty]
    private string processedDataset = "";

    public IRelayCommand ComputeCommand { get; }

    public HuberMethodViewModel(Dataset dataset, MainWindowViewModel mainViewModel)
    {
        _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        ComputeCommand = new AsyncRelayCommand(ComputeAsync);
    }

    private async Task ComputeAsync()
    {
        if (_dataset == null || _dataset.Values.Count == 0) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        Result = "Calculating...";
        ProcessedDataset = "";
        Progress = 0;
        _mainViewModel.Progress = 0;

        var progress = new Progress<int>(p =>
        {
            Progress = p;
            _mainViewModel.Progress = p;
        });

        try
        {
            var estimator = new HuberEstimator(TuningConstant);
            double result = await Task.Run(async () =>
            {
                double estimation = await estimator.ComputeAsync(_dataset, progress, _cts.Token);
                for (int i = 0; i <= 100; i++)
                {
                    ((IProgress<int>)progress).Report(i);
                    await Task.Delay(3, _cts.Token);
                }
                return estimation;
            }, _cts.Token);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Result = $"Result: {result:F2}";
                ProcessedDataset = $"Processed dataset: [{string.Join(", ", estimator.ProcessedValues.Take(20).Select(x => x.ToString("F3")))}...]";
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
