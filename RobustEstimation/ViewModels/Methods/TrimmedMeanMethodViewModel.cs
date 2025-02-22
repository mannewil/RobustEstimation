using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;

namespace RobustEstimation.ViewModels.Methods
{
    public partial class TrimmedMeanMethodViewModel : ViewModelBase
    {
        private readonly Dataset _dataset;
        private readonly MainWindowViewModel _mainViewModel;
        private CancellationTokenSource _cts;

        [ObservableProperty]
        private string result = "Not computed";

        [ObservableProperty]
        private double progress;

        [ObservableProperty]
        private double trimPercentage = 0.1; // По умолчанию 10%

        public TrimmedMeanMethodViewModel(Dataset dataset, MainWindowViewModel mainViewModel)
        {
            _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            ComputeCommand = new AsyncRelayCommand(ComputeTrimmedMeanAsync, () => _dataset.Values.Any());
            _dataset.PropertyChanged += (_, _) => ComputeCommand.NotifyCanExecuteChanged();
        }

        public IAsyncRelayCommand ComputeCommand { get; }

        private async Task ComputeTrimmedMeanAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            Result = "Calculating...";
            Progress = 0;
            _mainViewModel.Progress = 0;

            var progress = new Progress<int>(p =>
            {
                Progress = p;
                _mainViewModel.Progress = p;
            });

            try
            {
                var estimator = new TrimmedMeanEstimator(TrimPercentage);
                double result = await estimator.ComputeAsync(_dataset, progress, _cts.Token);

                await Dispatcher.UIThread.InvokeAsync(() => Result = $"Result: {result:F2}");
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
}
