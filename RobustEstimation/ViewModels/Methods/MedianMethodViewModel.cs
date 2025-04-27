using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;

namespace RobustEstimation.ViewModels.Methods;

public partial class MedianMethodViewModel : ViewModelBase
{
    private readonly Dataset _dataset;
    private readonly MainWindowViewModel _mainViewModel;
    private CancellationTokenSource _cts;

    [ObservableProperty]
    private string result = "Zatím nevypočítáno";

    [ObservableProperty]
    private double progress;

    public MedianMethodViewModel(Dataset dataset, MainWindowViewModel mainViewModel)
    {
        _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        ComputeCommand = new AsyncRelayCommand(ComputeMedianAsync, () => _dataset.Values.Any());
        _dataset.PropertyChanged += (_, _) => ComputeCommand.NotifyCanExecuteChanged();
    }

    public IAsyncRelayCommand ComputeCommand { get; }

    private async Task ComputeMedianAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        Result = "Počíta se...";
        Progress = 0;
        _mainViewModel.Progress = 0;

        var progress = new Progress<double>(p =>
        {
            Progress = p;
            _mainViewModel.Progress = p;
        });

        try
        {
            var values = _dataset.Values.OrderBy(x => x).ToArray();
            int count = values.Length;
            if (count == 0)
                throw new InvalidOperationException("Data nenalezena!");

            double median = await Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    ((IProgress<double>)progress).Report(((i + 1) / (double)count) * 100);
                }

                return count % 2 == 0
                    ? (values[count / 2 - 1] + values[count / 2]) / 2.0
                    : values[count / 2];
            }, _cts.Token);

            await Dispatcher.UIThread.InvokeAsync(() => Result = $"Výsledek: {median:F2}");
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() => Result = "Operace stornovaná.");
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => Result = $"Chyba: {ex.Message}");
        }
    }
}
