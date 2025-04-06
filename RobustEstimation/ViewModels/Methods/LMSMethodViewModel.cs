using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;
using OxyPlot.Annotations;
using OxyPlot.Series;
using OxyPlot;
using System.Collections.Generic;

namespace RobustEstimation.ViewModels.Methods;

public partial class LMSMethodViewModel : ViewModelBase, IGraphable
{
    private readonly Dataset _dataset;
    private readonly MainWindowViewModel _mainViewModel;
    private CancellationTokenSource _cts;
    private List<double> _squaredErrors = new();
    private double _lmsValue = 0;

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
            var result = await estimator.ComputeWithTimingAsync(_dataset, progress, _cts.Token);

            _squaredErrors = estimator.ProcessedErrors.ToList();
            _lmsValue = result.result;

            string processedData = $"[{string.Join(", ", _squaredErrors.Take(100))}]";
            string covarianceData = estimator.CovarianceMatrix != null
                ? $"[{estimator.CovarianceMatrix[0, 0]:F4}]"
                : "[N/A]";

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Result = $"Result: {result.result:F2} (Time: {result.duration.TotalMilliseconds} ms)";
                ProcessedErrors = $"Computed squared errors: {processedData}";
                CovarianceMatrix = $"Covariance matrix: {covarianceData}";
                _mainViewModel.IsGraphAvailable = true;
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

    public IEnumerable<Series> GetSeries()
    {
        var errorSeries = new ScatterSeries
        {
            Title = "Squared Errors",
            MarkerType = MarkerType.Circle,
            MarkerFill = OxyColors.OrangeRed
        };

        for (int i = 0; i < _squaredErrors.Count; i++)
            errorSeries.Points.Add(new ScatterPoint(i, _squaredErrors[i]));

        return new[] { errorSeries };
    }

    public double? GetHorizontalLineValue() => _lmsValue;
    public string? GetLineLabel() => $"LMS = {_lmsValue:F2}";
    public IEnumerable<Annotation> GetAnnotations() => Enumerable.Empty<Annotation>();
    public string GetGraphTitle() => "LMS Estimator Plot";
}
