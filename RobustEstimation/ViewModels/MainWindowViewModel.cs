using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;
using RobustEstimation.ViewModels.Methods;
using RobustEstimation.Models.Regression;
using RobustEstimation.Properties;
using HarfBuzzSharp;

namespace RobustEstimation.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<MethodType> Methods { get; } = new ObservableCollection<MethodType>(Enum.GetValues<MethodType>());

    public ObservableCollection<AppLanguage> Languages { get; } = new ObservableCollection<AppLanguage>(Enum.GetValues<AppLanguage>());

    [ObservableProperty]
    private MethodType selectedMethod;

    [ObservableProperty]
    private AppLanguage selectedLanguage;

    [ObservableProperty]
    private bool isGraphAvailable;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string result;

    [ObservableProperty]
    private string inputNumbers;

    [ObservableProperty]
    private string executionTime;

    [ObservableProperty]
    private string inputPlaceholder;

    [ObservableProperty]
    private ViewModelBase currentMethodViewModel;

    [ObservableProperty]
    private Dataset dataset;

    private readonly IWindowService _windowService;
    private CancellationTokenSource _cts;

    public MainWindowViewModel(IWindowService windowService)
    {
        Dataset = new Dataset();
        _windowService = windowService;
        SelectedMethod = MethodType.Median;
        var two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        SelectedLanguage = two switch
        {
            "cs" => AppLanguage.Czech,
            "ru" => AppLanguage.Russian,
            _ => AppLanguage.English
        };
        InputPlaceholder = "Number format: 1, 2, 3, 4…";
        UpdateMethodView();
    }

    [RelayCommand]
    private async Task LoadFileAsync()
    {
        var dialog = new OpenFileDialog();
        var result = await dialog.ShowAsync(new Window());
        if (result?.Length > 0)
        {
            var loaded = await FileManager.LoadFromFileAsync(result[0]);
            if (loaded.Points.Count > 0)
            {
                Dataset.SetPoints(loaded.Points);
                InputPlaceholder = "Pair format: x,y; x2,y2; …";
                InputNumbers = string.Join(";", loaded.Points.Select(p =>
                    $"{p.X.ToString(CultureInfo.InvariantCulture)},{p.Y.ToString(CultureInfo.InvariantCulture)}"));
                UpdateDataset();
            }
            else
            {
                Dataset.SetValues(loaded.Values);
                InputPlaceholder = "Number format: 1, 2, 3, …";
                InputNumbers = string.Join(", ", loaded.Values);
                UpdateDataset();
            }
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (Dataset == null
            || (Dataset.Values.Count == 0 && Dataset.Points.Count == 0))
            return;

        var dialog = new SaveFileDialog
        {
            DefaultExtension = ".txt",
            InitialFileName = $"{SelectedMethod}_out.txt",
            Filters = { new FileDialogFilter { Name = "Text files", Extensions = { "txt" } } }
        };
        var path = await dialog.ShowAsync(new Window());
        if (string.IsNullOrEmpty(path))
            return;

        var (methodParameter, processedData, computedResult, covarianceText, regression) = GetMethodDetails();

        // подставляем матрицу, если она есть
        if (!string.IsNullOrEmpty(covarianceText))
            processedData += "\n\nCovariance matrix: " + covarianceText;

        await FileManager.SaveToFileAsync(
            dataset: Dataset,
            path: path,
            selectedMethod: SelectedMethod,
            methodParameter: methodParameter,
            methodProcessedData: processedData,
            computedResult: computedResult,
            regression: regression  // сюда уйдёт либо null, либо ваш RegressionResult
        );
    }

    [RelayCommand]
    private async Task ComputeAsync()
    {
        UpdateDataset();

        if ((Dataset.Values.Count == 0 && Dataset.Points.Count == 0))
            return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        Progress = 0;
        Result = "Calculating...";
        ExecutionTime = ""; // Сброс предыдущего значения

        var progress = new Progress<int>(p => Progress = p);

        RobustEstimatorBase estimator = SelectedMethod switch
        {
            MethodType.Median => new MedianEstimator(),
            MethodType.Huber => new HuberEstimator(),
            MethodType.TrimmedMean => new TrimmedMeanEstimator(),
            MethodType.TheilSen => new TheilSenEstimator(),
            MethodType.LMS => new LMSEstimator(),
            _ => null
        };

        if (estimator != null)
        {
            try
            {
                var (computedResult, duration) = await estimator.ComputeWithTimingAsync(Dataset, progress, _cts.Token);
                Result = $"Result: {computedResult:F2} (Time: {duration.TotalMilliseconds} ms)";
            }
            catch (OperationCanceledException)
            {
                Result = "Calculation canceled.";
            }
        }
    }    

    [RelayCommand]
    private void ShowGraph()
    {
        if (_windowService == null)
        {
            Result = "_windowService is null!";
            return;
        }

        if (CurrentMethodViewModel == null)
        {
            Result = "CurrentMethodViewModel is null!";
            return;
        }

        _windowService.ShowGraphWindow(CurrentMethodViewModel);
    }

    partial void OnSelectedMethodChanged(MethodType newMethod)
    {
        UpdateMethodView();
        IsGraphAvailable = false;
        InputPlaceholder = SelectedMethod switch
        {
            MethodType.TheilSen or MethodType.LMS or MethodType.Huber
              => Resources.Placeholder_PairFormat,
            _ => Resources.Placeholder_NumberFormat
        };
    }

    partial void OnSelectedLanguageChanged(AppLanguage old, AppLanguage @new)
    {
        // 1) Устанавливаем новую культуру
        var ci = @new switch
        {
            AppLanguage.Czech => new CultureInfo("cs-CZ"),
            AppLanguage.Russian => new CultureInfo("ru-RU"),
            _ => new CultureInfo("en-US"),
        };
        Thread.CurrentThread.CurrentUICulture = ci;
        Thread.CurrentThread.CurrentCulture = ci;

        // 2) Пропатчим коллекции, чтобы ComboBox перебрал ItemTemplate заново
        var methodsBackup = Methods.ToArray();
        Methods.Clear();
        foreach (var m in methodsBackup) Methods.Add(m);

        var langsBackup = Languages.ToArray();
        Languages.Clear();
        foreach (var l in langsBackup) Languages.Add(l);

        OnPropertyChanged(nameof(InputPlaceholder));
        OnPropertyChanged(nameof(Methods));
        OnPropertyChanged(nameof(Languages));
        SelectedMethod = SelectedMethod; // триггерим OnSelectedMethodChanged
    }


    partial void OnInputNumbersChanged(string value)
    {
        UpdateDataset();
    }

    private void UpdateMethodView()
    {
        CurrentMethodViewModel = SelectedMethod switch
        {
            MethodType.Median => new MedianMethodViewModel(Dataset, this),
            MethodType.Huber => new HuberMethodViewModel(Dataset, this),
            MethodType.TrimmedMean => new TrimmedMeanMethodViewModel(Dataset, this),
            MethodType.TheilSen => new TheilSenMethodViewModel(Dataset, this),
            MethodType.LMS => new LMSMethodViewModel(Dataset, this),
            _ => null
        };
    }

    private void UpdateDataset()
    {
        if (Dataset == null)
            Dataset = new Dataset();

        // Если у нас VM, который поддерживает переключение plain/regen mode...
        if (CurrentMethodViewModel is TheilSenMethodViewModel theilVm && theilVm.IsRegressionMode
         || CurrentMethodViewModel is LMSMethodViewModel lmsVm && lmsVm.IsRegressionMode
         || CurrentMethodViewModel is HuberMethodViewModel huberVm && huberVm.IsRegressionMode)
        {
            // парный ввод: x,y; x2,y2; …
            var pairs = InputNumbers?
                .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length == 2
                                && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                                && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                .Select(parts => (
                    X: double.Parse(parts[0], CultureInfo.InvariantCulture),
                    Y: double.Parse(parts[1], CultureInfo.InvariantCulture)))
                .ToList()
                ?? new List<(double, double)>();

            Dataset.SetPoints(pairs);
        }
        else
        {
            // обычный одномерный ввод v1, v2, v3, …
            var values = InputNumbers?
                .Split(new[] { ' ', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var num)
                               ? num
                               : (double?)null)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .ToList()
                ?? new List<double>();

            Dataset.SetValues(values);
        }
    }

    private (string methodParameter,
          string processedData,
          string computedResult,
          string covarianceText,
          RegressionResult? regression) GetMethodDetails()
    {
        string methodParameter = "", processedData = "", covarianceText = "";
        string computedResult = "";
        RegressionResult? regression = null;

        switch (CurrentMethodViewModel)
        {
            case MedianMethodViewModel md:
                methodParameter = "Median Estimator";
                computedResult = md.Result;
                break;

            case TrimmedMeanMethodViewModel tm:
                methodParameter = $"Trim Fraction: {tm.TrimPercentage * 100:F0}%";
                processedData = string.Join(", ", tm.ProcessedData);
                computedResult = tm.Result;
                covarianceText = tm.CovarianceMatrix;
                break;

            case HuberMethodViewModel hub:
                methodParameter = $"Delta: {hub.TuningConstant}";
                processedData = string.Join(", ", hub.ProcessedData);
                computedResult = hub.Result;
                covarianceText = hub.CovarianceMatrix;
                regression = hub.LastRegressionResult;
                break;

            case TheilSenMethodViewModel th:
                methodParameter = th.IsRegressionMode ? "Theil‑Sen Regression" : "Theil‑Sen Estimator";
                processedData = string.Join(", ", th.ProcessedSlopes);
                computedResult = th.Result;
                if (th.IsRegressionMode) regression = th.LastRegressionResult;
                break;

            case LMSMethodViewModel lms:
                methodParameter = lms.IsRegressionMode ? "LMS Regression" : "LMS Estimator";
                processedData = string.Join(", ", lms.ProcessedErrors);
                computedResult = lms.Result;
                if (lms.IsRegressionMode) regression = lms.LastRegressionResult;
                break;
        }

        return (methodParameter, processedData, computedResult, covarianceText, regression);
    }

}
