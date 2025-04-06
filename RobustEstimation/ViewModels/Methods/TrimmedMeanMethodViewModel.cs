using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using RobustEstimation.Models;

namespace RobustEstimation.ViewModels.Methods;

public partial class TrimmedMeanMethodViewModel : ViewModelBase, IGraphable
{
    private readonly Dataset _dataset;
    private readonly MainWindowViewModel _mainViewModel;
    private CancellationTokenSource _cts;

    private List<double> _allData = new();
    private List<double> _trimmedData = new();
    private double _trimmedMean = 0;

    [ObservableProperty]
    private string result = "Not computed";

    [ObservableProperty]
    private string processedDataset = "";

    [ObservableProperty]
    private string covarianceMatrix = "";

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private double trimPercentage = 0.1;

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
        ProcessedDataset = "";
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
            var estimator = new TrimmedMeanEstimator(TrimPercentage);
            var result = await estimator.ComputeWithTimingAsync(_dataset, progress, _cts.Token);

            _allData = _dataset.Values.ToList();
            _trimmedData = estimator.ProcessedData.ToList();
            _trimmedMean = result.result;

            string processedData = $"[{string.Join(", ", _trimmedData.Take(100))}]";
            string covarianceMatrixFormatted = estimator.CovarianceMatrix != null
                ? $"{estimator.CovarianceMatrix[0, 0]:F4}"
                : "Error calculating covariance";

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Result = $"Result: {result.result:F2} (Time: {result.duration.TotalMilliseconds} ms)";
                ProcessedDataset = $"Processed dataset: {processedData}";
                CovarianceMatrix = $"Covariance matrix: {covarianceMatrixFormatted}";
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
        var fullSeries = new ScatterSeries
        {
            Title = "All Data",
            MarkerType = MarkerType.Circle,
            MarkerFill = OxyColors.Gray
        };

        for (int i = 0; i < _allData.Count; i++)
            fullSeries.Points.Add(new ScatterPoint(i, _allData[i]));

        var trimmedSeries = new ScatterSeries
        {
            Title = "Trimmed Data",
            MarkerType = MarkerType.Circle,
            MarkerFill = OxyColors.Blue
        };

        for (int i = 0; i < _trimmedData.Count; i++)
            trimmedSeries.Points.Add(new ScatterPoint(i, _trimmedData[i]));

        return new[] { fullSeries, trimmedSeries };
    }

    public double? GetHorizontalLineValue() => _trimmedMean;
    public string? GetLineLabel() => $"Trimmed Mean = {_trimmedMean:F2}";
    public IEnumerable<Annotation> GetAnnotations() => Enumerable.Empty<Annotation>();
    public string GetGraphTitle() => "Trimmed Mean Plot";
}
