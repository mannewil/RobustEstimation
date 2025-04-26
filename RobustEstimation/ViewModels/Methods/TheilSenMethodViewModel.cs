using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Annotations;
using RobustEstimation.Models;
using RobustEstimation.Models.Regression;
using System.Globalization;

namespace RobustEstimation.ViewModels.Methods;

public partial class TheilSenMethodViewModel : ViewModelBase, IGraphable
{
    private readonly Dataset _dataset;
    private readonly MainWindowViewModel _mainViewModel;
    private CancellationTokenSource _cts;
    public RegressionResult LastRegressionResult { get; private set; }

    private double _medianSlope;
    private TimeSpan _medianSlopeDuration;
    private RegressionResult res;

    [ObservableProperty]
    private string result = "Not computed";

    [ObservableProperty]
    private string processedSlopes = "";

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private bool isRegressionMode;

    [ObservableProperty]
    private bool canCompute;

    public string InputPlaceholder =>
        IsRegressionMode
            ? "Enter points as x,y; x2,y2; …"
            : "Enter numbers as v1, v2, v3, …";

    public IAsyncRelayCommand ComputeCommand { get; }

    public TheilSenMethodViewModel(Dataset dataset, MainWindowViewModel mainViewModel)
    {
        _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

        ComputeCommand = new AsyncRelayCommand(
            ComputeTheilSenAsync,
            () => CanCompute
        );

        _dataset.PropertyChanged += (_, _) => UpdateCanCompute();
        this.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsRegressionMode))
            {
                OnPropertyChanged(nameof(InputPlaceholder));
                UpdateCanCompute();
            }
        };

        UpdateCanCompute();
    }

    partial void OnIsRegressionModeChanged(bool _)
    {
        OnPropertyChanged(nameof(InputPlaceholder));
        UpdateCanCompute();
    }

    private void UpdateCanCompute()
    {
        CanCompute = IsRegressionMode
            ? _dataset.Points.Any()
            : _dataset.Values.Any();
        ComputeCommand.NotifyCanExecuteChanged();
    }

    private async Task ComputeTheilSenAsync()
    {
        if (!CanCompute) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        Result = "Calculating...";
        ProcessedSlopes = "";
        Progress = 0;
        _mainViewModel.Progress = 0;

        var prog = new Progress<int>(p =>
        {
            Progress = p;
            _mainViewModel.Progress = p;
        });

        if (IsRegressionMode)
        {
            var reg = new TheilSenRegressionEstimator();
            res = await reg.FitAsync(_dataset.Points, prog, _cts.Token);
            LastRegressionResult = res;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Result =
                    $"y = {res.Slope:F2}x + {res.Intercept:F2}  " +
                    $"(R² = {res.RSquared:F3}), (med sq res: {res.MedianSquaredResidual:F2}, Time: {res.Elapsed.TotalMilliseconds:F0} ms)";

                ProcessedSlopes =
                    $"Slopes: [{string.Join(", ", reg.ProcessedSlopes.Select(x => x.ToString("F2", CultureInfo.InvariantCulture)))}]";
                _mainViewModel.IsGraphAvailable = true;
            });
        }
        else
        {
            var est = new TheilSenEstimator();
            var (slope, duration) =
                await est.ComputeWithTimingAsync(_dataset, prog, _cts.Token);

            _medianSlope = slope;
            _medianSlopeDuration = duration;
            LastRegressionResult = null;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Result =
                    $"Median slope: {_medianSlope:F2} ({_medianSlopeDuration.TotalMilliseconds:F0} ms)";
                ProcessedSlopes =
                    $"Slopes: [{string.Join(", ", est.ProcessedSlopes.Take(20))}…]";
                _mainViewModel.IsGraphAvailable = true;
            });
        }
    }

    public IEnumerable<Series> GetSeries()
    {
        if (IsRegressionMode)
        {
            var scatter = new ScatterSeries
            {
                Title = "Data Points",
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColors.SteelBlue
            };
            foreach (var (x, y) in _dataset.Points)
                scatter.Points.Add(new ScatterPoint(x, y));

            var line = new LineSeries
            {
                Title = "Theil‑Sen Regression",
                Color = OxyColors.Red,
                StrokeThickness = 2
            };
            double minX = _dataset.Points.Min(p => p.X);
            double maxX = _dataset.Points.Max(p => p.X);
            line.Points.Add(new DataPoint(minX, res.Slope * minX + res.Intercept));
            line.Points.Add(new DataPoint(maxX, res.Slope * maxX + res.Intercept));

            return new Series[] { scatter, line };
        }
        else
        {
            var series = new LineSeries
            {
                Title = "Values",
                Color = OxyColors.Gray,
                MarkerType = MarkerType.Circle
            };
            for (int i = 0; i < _dataset.Values.Count; i++)
                series.Points.Add(new DataPoint(i, _dataset.Values[i]));
            return new[] { series };
        }
    }

    public IEnumerable<Annotation> GetAnnotations()
    {
        if (IsRegressionMode)
            yield break;

        yield return new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = _medianSlope,
            Color = OxyColors.Red,
            Text = $"Median slope = {_medianSlope:F2}",
            TextHorizontalAlignment = HorizontalAlignment.Left,
            TextVerticalAlignment = VerticalAlignment.Top,
            TextMargin = 4,
        };
    }

    public string GetGraphTitle() =>
        IsRegressionMode
            ? "Theil‑Sen Regression"
            : "Theil‑Sen Median Slope Plot";

    public double? GetHorizontalLineValue() =>
       IsRegressionMode ? (double?)null : _medianSlope;

    public string? GetLineLabel() =>
        IsRegressionMode ? null : $"Median slope = {_medianSlope:F2}";
}
