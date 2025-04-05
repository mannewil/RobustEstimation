using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;
using RobustEstimation.ViewModels.Methods;

namespace RobustEstimation.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<string> Methods { get; } = new()
    {
        "Median", "Huber", "Trimmed Mean", "Theil-Sen", "LMS"
    };

    public ObservableCollection<string> Languages { get; } = new()
    {
        "English", "Czech", "Russian"
    };

    [ObservableProperty]
    private string selectedMethod;

    [ObservableProperty]
    private string selectedLanguage;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string result;

    [ObservableProperty]
    private string inputNumbers;

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
        SelectedMethod = Methods[0];
        SelectedLanguage = Languages[0];
    }

    [RelayCommand]
    private async Task LoadFileAsync()
    {
        var dialog = new OpenFileDialog();
        var result = await dialog.ShowAsync(new Window());

        if (result?.Length > 0)
        {
            var loadedDataset = await FileManager.LoadFromFileAsync(result[0]);
            InputNumbers = string.Join(", ", loadedDataset.Values);
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (Dataset == null || Dataset.Values.Count == 0)
            return;

        var dialog = new SaveFileDialog
        {
            DefaultExtension = ".txt",
            InitialFileName = $"{SelectedMethod}_out.txt",
            Filters = { new FileDialogFilter { Name = "Text files", Extensions = { "txt" } } }
        };

        var result = await dialog.ShowAsync(new Window());
        if (string.IsNullOrEmpty(result))
            return;

        var (methodParameter, methodProcessedDataset, computedResult, covarianceMatrixText) = GetMethodDetails();
        methodProcessedDataset += $"\n\n{covarianceMatrixText}";

        await FileManager.SaveToFileAsync(Dataset, result, SelectedMethod, methodParameter, methodProcessedDataset, computedResult);
    }

    [RelayCommand]
    private async Task ComputeAsync()
    {
        if (Dataset == null || Dataset.Values.Count == 0)
        {
            UpdateDataset();
        }

        if (Dataset.Values.Count == 0) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        Progress = 0;
        Result = "Calculating...";

        var progress = new Progress<int>(p => Progress = p);

        RobustEstimatorBase estimator = SelectedMethod switch
        {
            "Median" => new MedianEstimator(),
            "Huber" => new HuberEstimator(),
            "Trimmed Mean" => new TrimmedMeanEstimator(),
            "Theil-Sen" => new TheilSenEstimator(),
            "LMS" => new LMSEstimator(),
            _ => null
        };

        if (estimator != null)
        {
            try
            {
                double computedResult = await estimator.ComputeAsync(Dataset, progress, _cts.Token);
                Result = $"Result: {computedResult:F2}";
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

    partial void OnSelectedMethodChanged(string value)
    {
        UpdateMethodView();
    }

    partial void OnInputNumbersChanged(string value)
    {
        UpdateDataset();
    }

    private void UpdateMethodView()
    {
        CurrentMethodViewModel = SelectedMethod switch
        {
            "Median" => new MedianMethodViewModel(Dataset, this),
            "Huber" => new HuberMethodViewModel(Dataset, this),
            "Trimmed Mean" => new TrimmedMeanMethodViewModel(Dataset, this),
            "Theil-Sen" => new TheilSenMethodViewModel(Dataset, this),
            "LMS" => new LMSMethodViewModel(Dataset, this),
            _ => null
        };
    }

    private void UpdateDataset()
    {
        if (Dataset == null)
            Dataset = new Dataset();

        var values = InputNumbers?.Split(new[] { ' ', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) ? num : (double?)null)
                                 .Where(n => n.HasValue)
                                 .Select(n => n.Value)
                                 .ToList() ?? new List<double>();

        Dataset.SetValues(values);
    }

    private (string methodParameter, string methodProcessedDataset, double computedResult, string covarianceMatrixText) GetMethodDetails()
    {
        string methodParameter = "", methodProcessedDataset = "", covarianceMatrixText = "";
        double computedResult = double.NaN;

        switch (CurrentMethodViewModel)
        {
            case TrimmedMeanMethodViewModel trimmedMeanVM:
                methodParameter = $"Trim Fraction: {trimmedMeanVM.TrimPercentage * 100:F0}%";
                methodProcessedDataset = string.Join(", ", trimmedMeanVM.ProcessedDataset);
                computedResult = ParseResult(trimmedMeanVM.Result);
                covarianceMatrixText = $"{trimmedMeanVM.CovarianceMatrix}";
                break;

            case HuberMethodViewModel huberVM:
                methodParameter = $"Delta: {huberVM.TuningConstant}";
                methodProcessedDataset = string.Join(", ", huberVM.ProcessedDataset);
                computedResult = ParseResult(huberVM.Result);
                covarianceMatrixText = $"{huberVM.CovarianceMatrix}";
                break;

            case LMSMethodViewModel lmsVM:
                methodParameter = "LMS Estimator (default settings)";
                methodProcessedDataset = string.Join(", ", lmsVM.ProcessedErrors);
                computedResult = ParseResult(lmsVM.Result);
                covarianceMatrixText = $"{lmsVM.CovarianceMatrix}";
                break;

            case TheilSenMethodViewModel theilSenVM:
                methodParameter = "Theil-Sen Estimator";
                methodProcessedDataset = string.Join(", ", theilSenVM.ProcessedSlopes);
                computedResult = ParseResult(theilSenVM.Result);
                break;

            case MedianMethodViewModel medianVM:
                methodParameter = "Median Estimator";
                computedResult = ParseResult(medianVM.Result);
                break;
        }

        return (methodParameter, methodProcessedDataset, computedResult, covarianceMatrixText);
    }

    private double ParseResult(string resultText)
    {
        if (string.IsNullOrEmpty(resultText)) return double.NaN;
        resultText = resultText.Replace("Result:", "").Trim().Replace(',', '.');
        return double.TryParse(resultText, NumberStyles.Any, CultureInfo.InvariantCulture, out var res) ? res : double.NaN;
    }
}
