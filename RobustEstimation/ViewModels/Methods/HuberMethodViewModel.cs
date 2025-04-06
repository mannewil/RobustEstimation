using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot.Annotations;
using OxyPlot.Series;
using OxyPlot;
using RobustEstimation.Models;

namespace RobustEstimation.ViewModels.Methods;

public partial class HuberMethodViewModel : ViewModelBase, IGraphable
{
    private readonly MainWindowViewModel _mainViewModel;
    private CancellationTokenSource _cts;
    private readonly Dataset _dataset;
    private List<double> _allData = new();
    private List<double> _processedData = new();
    private double _mean = 0;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private double tuningConstant = 1.345;

    [ObservableProperty]
    private string result = "Not computed";

    [ObservableProperty]
    private string processedDataset = "";

    [ObservableProperty]
    private string covarianceMatrix = "";

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
            var estimator = new HuberEstimator(TuningConstant);
            double result = await Task.Run(async () =>
            {
                double estimation = await estimator.ComputeAsync(_dataset, progress, _cts.Token);
                for (int i = 0; i <= 100; i++)
                {
                    ((IProgress<int>)progress).Report(i);
                    await Task.Delay(1, _cts.Token);
                }
                return estimation;
            }, _cts.Token);

            _allData = _dataset.Values.ToList();
            _processedData = estimator.ProcessedValues.ToList();
            _mean = result;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Result = $"Result: {result:F2}";
                ProcessedDataset = $"Processed dataset: [{string.Join(", ", estimator.ProcessedValues.Take(100).Select(x => x.ToString("F3")))}]";
                CovarianceMatrix = $"Covariance matrix: {estimator.CovarianceMatrix[0, 0].ToString("F4", CultureInfo.InvariantCulture)}";
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
        var originalSeries = new LineSeries
        {
            Title = "Original",
            Color = OxyColors.Gray
        };

        for (int i = 0; i < _allData.Count; i++)
            originalSeries.Points.Add(new DataPoint(i, _allData[i]));

        var processedSeries = new LineSeries
        {
            Title = "Huber Adjusted",
            Color = OxyColors.SteelBlue
        };

        for (int i = 0; i < _processedData.Count; i++)
            processedSeries.Points.Add(new DataPoint(i, _processedData[i]));

        return new[] { originalSeries, processedSeries };
    }

    public double? GetHorizontalLineValue() => _mean;
    public string? GetLineLabel() => $"Huber = {_mean:F2}";
    public IEnumerable<Annotation> GetAnnotations() => Enumerable.Empty<Annotation>();
    public string GetGraphTitle() => "Huber Estimator Plot";
}
