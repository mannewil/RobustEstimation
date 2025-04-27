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
    public partial class LMSMethodViewModel : ViewModelBase, IGraphable
    {
        private readonly Dataset _dataset;
        private readonly MainWindowViewModel _mainVM;
        private CancellationTokenSource? _cts;
        public RegressionResult LastRegressionResult { get; private set; }

        // storage for plotting
        private List<double> _squaredErrors = new();
        private double _lmsThreshold = 0;
        private List<(double x, double y)> _points = new();
        private double _regSlope = 0, _regIntercept = 0;

        [ObservableProperty]
        private bool isRegressionMode;

        [ObservableProperty]
        private bool canCompute;

        [ObservableProperty]
        private double progress;

        [ObservableProperty]
        private string result = "Zatím nevypočítáno";

        [ObservableProperty]
        private string processedErrors = "";

        [ObservableProperty]
        private string covarianceMatrix = "";

        public string InputPlaceholder =>
            IsRegressionMode
                ? "Format čísel: x1,y1; x2,y2; ... nebo x1 y1; x2 y2; ..."
                : "Format čísel: x1, x2, x3, x4... nebo x1 x2 x3 x4...";

        public IAsyncRelayCommand ComputeCommand { get; }

        public LMSMethodViewModel(Dataset dataset, MainWindowViewModel mainVM)
        {
            _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            this._mainVM = _mainVM ?? throw new ArgumentNullException(nameof(_mainVM));

            // single command, enable/disable via CanCompute
            ComputeCommand = new AsyncRelayCommand(ComputeLMSAsync);

            // react to data changes
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

        partial void OnIsRegressionModeChanged(bool _)
        {
            ComputeCommand.NotifyCanExecuteChanged();
            _mainVM.InputPlaceholder = InputPlaceholder;
        }

        private void UpdateCanCompute()
        {
            CanCompute = IsRegressionMode
                ? _dataset.Points.Any()
                : _dataset.Values.Any();
        }

        private async Task ComputeLMSAsync()
        {
            if (!CanCompute)
                return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            Result = "Počíta se...";
            ProcessedErrors = "";
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
                // Regression mode: fit line by LMS criterion
                var reg = new LMSRegressionEstimator();
                var res = await reg.FitAsync(_dataset.Points, prog, _cts.Token);

                _regSlope = res.Slope;
                _regIntercept = res.Intercept;
                _lmsThreshold = res.MedianSquaredResidual;
                _points = _dataset.Points.ToList();
                LastRegressionResult = res;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Result = $"y = {res.Slope:F2}x + {res.Intercept:F2}  " +
                    $"(R² = {res.RSquared:F3}, Medián čtvercových reziduí: {res.MedianSquaredResidual:F2}, Čás: {res.Elapsed.TotalMilliseconds:F0} ms)";
                    ProcessedErrors =
                        $"Čtvercové rezidui: [{string.Join(", ", reg.ProcessedResiduals.Select(x => x.ToString("F2", CultureInfo.InvariantCulture)))}]";
                });
            }
            else
            {
                // Plain mode: compute median-of-squares
                var est = new LMSEstimator();
                var (lmsValue, duration) = await est.ComputeWithTimingAsync(_dataset, prog, _cts.Token);

                _squaredErrors = est.ProcessedErrors.ToList();
                _lmsThreshold = lmsValue;
                LastRegressionResult = null;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Result = $"LMS: {lmsValue:F2} ({duration.TotalMilliseconds:F0} ms)";
                    ProcessedErrors =
                        $"Čtvrcové chyby: [{string.Join(", ", _squaredErrors.Take(20))}…]";
                });
            }

            // now graph is available
            _mainVM.IsGraphAvailable = true;
        }

        public IEnumerable<Series> GetSeries()
        {
            if (IsRegressionMode)
            {
                // show raw points + fitted line
                var ptsSeries = new ScatterSeries
                {
                    Title = "Data Points",
                    MarkerType = MarkerType.Circle,
                    MarkerFill = OxyColors.LightSkyBlue
                };
                for (int i = 0; i < _points.Count; i++)
                    ptsSeries.Points.Add(new ScatterPoint(_points[i].x, _points[i].y));

                // line from minX to maxX
                double minX = _points.Min(p => p.x);
                double maxX = _points.Max(p => p.x);
                var line = new LineSeries
                {
                    Title = "LMS Regression",
                    Color = OxyColors.Orange,
                    StrokeThickness = 2
                };
                line.Points.Add(new DataPoint(minX, _regSlope * minX + _regIntercept));
                line.Points.Add(new DataPoint(maxX, _regSlope * maxX + _regIntercept));

                return new Series[] { ptsSeries, line };
            }
            else
            {
                // plot squared errors vs index
                var errSeries = new ScatterSeries
                {
                    Title = "Squared Errors",
                    MarkerType = MarkerType.Circle,
                    MarkerFill = OxyColors.OrangeRed
                };
                for (int i = 0; i < _squaredErrors.Count; i++)
                    errSeries.Points.Add(new ScatterPoint(i, _squaredErrors[i]));

                return new[] { errSeries };
            }
        }

        public IEnumerable<Annotation> GetAnnotations()
        {
            // draw horizontal line at LMS threshold
            return new[]
            {
                new LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = _lmsThreshold,
                    Color = OxyColors.Red,
                    Text = IsRegressionMode
                        ? $"Med sq res = {_lmsThreshold:F2}"
                        : $"LMS = {_lmsThreshold:F2}",
                    TextHorizontalAlignment = HorizontalAlignment.Left,
                    TextVerticalAlignment = VerticalAlignment.Top,
                    TextMargin = 4
                }
            };
        }

        public string GetGraphTitle()
            => IsRegressionMode
                ? "LMS Regression Plot"
                : "LMS Error Plot";

        public double? GetHorizontalLineValue() => _lmsThreshold;
        public string? GetLineLabel()
            => IsRegressionMode
                ? $"Med sq res = {_lmsThreshold:F2}"
                : $"LMS = {_lmsThreshold:F2}";
    }
}
