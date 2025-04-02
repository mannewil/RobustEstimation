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
    public partial class LMSMethodViewModel : ViewModelBase
    {
        private readonly Dataset _dataset;
        private readonly MainWindowViewModel _mainViewModel;
        private CancellationTokenSource _cts;

        [ObservableProperty]
        private string result = "Not computed";

        [ObservableProperty]
        private string processedErrors = "";

        [ObservableProperty]
        private string covarianceMatrix = "";

        [ObservableProperty]
        private double progress;

        public LMSMethodViewModel(Dataset dataset, MainWindowViewModel mainViewModel)
        {
            _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            ComputeCommand = new AsyncRelayCommand(ComputeLMSAsync, () => _dataset.Values.Any());
            _dataset.PropertyChanged += (_, _) => ComputeCommand.NotifyCanExecuteChanged();
        }

        public IAsyncRelayCommand ComputeCommand { get; }

        private async Task ComputeLMSAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            Result = "Calculating...";
            ProcessedErrors = "";
            CovarianceMatrix = "";
            Progress = 0;
            _mainViewModel.Progress = 0;

            var progress = new Progress<int>(p =>
            {
                Progress = p;
                _mainViewModel.Progress = p;
            });

            try
            {
                var estimator = new LMSEstimator();
                double result = await estimator.ComputeAsync(_dataset, progress, _cts.Token);

                string processedData = $"[{string.Join(", ", estimator.ProcessedErrors.Take(100))}]";
                string covarianceData = estimator.CovarianceMatrix != null
                    ? $"[{estimator.CovarianceMatrix[0, 0]:F4}]"
                    : "[N/A]";

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Result = $"Result: {result:F2}";
                    ProcessedErrors = $"Computed squared errors: {processedData}";
                    CovarianceMatrix = $"Covariance matrix: {covarianceData}";
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
}
