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

namespace RobustEstimation.ViewModels.Methods
{
    public partial class TrimmedMeanMethodViewModel : ViewModelBase, IGraphable
    {
        private readonly Dataset _dataset;
        private readonly MainWindowViewModel _mainVM;
        private CancellationTokenSource _cts;

        private List<double> _allData = new();
        private List<double> _trimmed = new();
        private double _mean = 0;

        [ObservableProperty] 
        private double trimPercentage = 0.1;
        [ObservableProperty] 
        private double progress;
        [ObservableProperty] 
        private string result = "Not computed";
        [ObservableProperty] 
        private string processedData = "";
        [ObservableProperty] 
        private string covarianceMatrix = "";

        public IAsyncRelayCommand ComputeCommand { get; }

        public TrimmedMeanMethodViewModel(Dataset dataset, MainWindowViewModel mainVM)
        {
            _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            _mainVM = mainVM ?? throw new ArgumentNullException(nameof(mainVM));
            ComputeCommand = new AsyncRelayCommand(ComputeAsync, () => _dataset.Values.Any());
            _dataset.PropertyChanged += (_, __) => ComputeCommand.NotifyCanExecuteChanged();
        }

        private async Task ComputeAsync()
        {
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

            try
            {
                var est = new TrimmedMeanEstimator(TrimPercentage);
                var (mean, duration) = await est.ComputeWithTimingAsync(_dataset, prog, _cts.Token);

                _allData = _dataset.Values.ToList();
                _trimmed = est.ProcessedData.ToList();
                _mean = mean;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Result = $"Result: {_mean:F2}  (Time: {duration.TotalMilliseconds:F0} ms)";
                    ProcessedData = $"[ {string.Join(", ", _trimmed.Select(x => x.ToString("F2", CultureInfo.InvariantCulture)))} ]";
                    CovarianceMatrix = FormatMatrix(est.CovarianceMatrix);
                    _mainVM.IsGraphAvailable = true;
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
            var all = new ScatterSeries { Title = "All Data", MarkerType = MarkerType.Circle };
            for (int i = 0; i < _allData.Count; i++)
                all.Points.Add(new ScatterPoint(i, _allData[i]));

            var trim = new ScatterSeries { Title = "Trimmed", MarkerType = MarkerType.Circle, MarkerFill = OxyColors.Blue };
            for (int i = 0; i < _trimmed.Count; i++)
                trim.Points.Add(new ScatterPoint(i, _trimmed[i]));

            return new[] { all, trim };
        }

        public IEnumerable<Annotation> GetAnnotations()
        {
            return new[]
            {
                new LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = _mean,
                    Color = OxyColors.Red,
                    Text = $"Trimmed Mean = {_mean:F2}",
                    TextHorizontalAlignment = HorizontalAlignment.Left,
                    TextVerticalAlignment = VerticalAlignment.Top,
                    TextMargin = 4
                }
            };
        }

        public string GetGraphTitle() => "Trimmed Mean Plot";
        public double? GetHorizontalLineValue() => _mean;
        public string? GetLineLabel() => $"Mean = {_mean:F2}";
    }
}
