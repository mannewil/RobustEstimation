// HuberMethodViewModel.cs
using System;
using System.Collections.Generic;
using System.Globalization;
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
using RobustEstimation.Models.Regression;

namespace RobustEstimation.ViewModels.Methods
{
    public partial class HuberMethodViewModel : ViewModelBase, IGraphable
    {
        private readonly Dataset _dataset;
        private readonly MainWindowViewModel _mainVM;
        private CancellationTokenSource _cts;

        // regression result for save/export
        public RegressionResult? LastRegressionResult { get; private set; }

        // for simple M‑estimate
        private List<double> _orig = new(), _adj = new();
        private double _mean;

        // for regression
        private List<(double x, double y)> _pts = new();
        private double _slope, _intercept;

        [ObservableProperty]
        private double tuningConstant = 1.345;

        [ObservableProperty]
        private bool isRegressionMode;

        [ObservableProperty]
        private bool canCompute;

        [ObservableProperty]
        private double progress;

        [ObservableProperty]
        private string result = "Not computed";

        [ObservableProperty]
        private string processedData = "";

        [ObservableProperty]
        private string covarianceMatrix = "";
        public IAsyncRelayCommand ComputeCommand { get; }

        public string InputPlaceholder =>
            IsRegressionMode
                ? "Enter points as x,y; x2,y2; …"
                : "Enter numbers as v1, v2, v3; …";

        public HuberMethodViewModel(Dataset dataset, MainWindowViewModel mainVM)
        {
            _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            _mainVM = mainVM ?? throw new ArgumentNullException(nameof(mainVM));

            ComputeCommand = new AsyncRelayCommand(ComputeAsync);
            _dataset.PropertyChanged += (_, __) => UpdateCanCompute();
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

        partial void OnIsRegressionModeChanged(bool _) =>
            ComputeCommand.NotifyCanExecuteChanged();

        private void UpdateCanCompute()
        {
            CanCompute = IsRegressionMode
                ? _dataset.Points.Any()
                : _dataset.Values.Any();
        }

        private async Task ComputeAsync()
        {
            if (!CanCompute) return;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            Result = "Calculating...";
            ProcessedData = "";
            CovarianceMatrix = "";
            Progress = 0;
            _mainVM.Progress = 0;
            var prog = new Progress<int>(p =>
            {
                Progress = p;
                _mainVM.Progress = p;
            });

            if (IsRegressionMode)
            {
                // —— Huber regression via IRLS ——
                var reg = new HuberRegressionEstimator(TuningConstant);
                var res = await reg.FitAsync(_dataset.Points, prog, _cts.Token);

                _pts = _dataset.Points.ToList();
                _slope = res.Slope;
                _intercept = res.Intercept;
                LastRegressionResult = res;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Result =
                        $"y = {_slope:F2}x + {_intercept:F2}  " +
                        $"(R² = {res.RSquared:F3}, Median squared residual: {res.MedianSquaredResidual:F2}, Time: {res.Elapsed.TotalMilliseconds:F0} ms)";
                    ProcessedData = "–";
                    covarianceMatrix = FormatMatrix(new[,] { { res.MedianSquaredResidual } });
                    _mainVM.IsGraphAvailable = true;
                });
            }
            else
            {
                // —— Simple Huber M‑estimate ——
                var est = new HuberEstimator(TuningConstant);
                var (mean, duration) = await est.ComputeWithTimingAsync(_dataset, prog, _cts.Token);

                _orig = _dataset.Values.ToList();
                _adj = est.ProcessedValues.ToList();
                _mean = mean;
                LastRegressionResult = null;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Result =
                        $"Result: {_mean:F2}  (Time: {duration.TotalMilliseconds:F0} ms)";
                    ProcessedData = $"[ {string.Join(", ", _adj.Select(x => x.ToString("F2", CultureInfo.InvariantCulture)))} ]";
                    covarianceMatrix = FormatMatrix(est.CovarianceMatrix);
                    _mainVM.IsGraphAvailable = true;
                });
            }
        }

        public IEnumerable<Series> GetSeries()
        {
            if (IsRegressionMode)
            {
                var ptsSeries = new ScatterSeries
                {
                    Title = "Data Points",
                    MarkerType = MarkerType.Circle
                };
                foreach (var p in _pts)
                    ptsSeries.Points.Add(new ScatterPoint(p.x, p.y));

                var line = new LineSeries
                {
                    Title = "Huber Regression",
                    Color = OxyColors.Red
                };
                if (_pts.Any())
                {
                    var xs = _pts.Select(p => p.x);
                    double xmin = xs.Min(), xmax = xs.Max();
                    line.Points.Add(new DataPoint(xmin, _slope * xmin + _intercept));
                    line.Points.Add(new DataPoint(xmax, _slope * xmax + _intercept));
                }

                return new Series[] { ptsSeries, line };
            }
            else
            {
                var origSeries = new LineSeries
                {
                    Title = "Original",
                    Color = OxyColors.Gray
                };
                for (int i = 0; i < _orig.Count; i++)
                    origSeries.Points.Add(new DataPoint(i, _orig[i]));

                var adjSeries = new LineSeries
                {
                    Title = "Adjusted",
                    Color = OxyColors.SteelBlue
                };
                for (int i = 0; i < _adj.Count; i++)
                    adjSeries.Points.Add(new DataPoint(i, _adj[i]));

                return new Series[] { origSeries, adjSeries };
            }
        }

        public IEnumerable<Annotation> GetAnnotations()
        {
            if (IsRegressionMode)
                return Enumerable.Empty<Annotation>();

            return new[]
            {
                new LineAnnotation
                {
                    Type                     = LineAnnotationType.Horizontal,
                    Y                        = _mean,
                    Color                    = OxyColors.Red,
                    Text                     = $"Huber Mean = {_mean:F2}",
                    TextHorizontalAlignment  = HorizontalAlignment.Left,
                    TextVerticalAlignment    = VerticalAlignment.Top,
                    TextMargin               = 4
                }
            };
        }

        public string GetGraphTitle() =>
            IsRegressionMode ? "Huber Regression" : "Huber M‑Estimate";

        public double? GetHorizontalLineValue() =>
            IsRegressionMode ? null : (double?)_mean;

        public string? GetLineLabel() =>
            IsRegressionMode ? null : $"Mean = {_mean:F2}";
    }
}
