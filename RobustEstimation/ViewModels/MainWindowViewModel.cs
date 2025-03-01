using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;
using RobustEstimation.ViewModels.Methods;
using RobustEstimation.Views.Methods;

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

    public ICommand LoadFileCommand { get; }
    public ICommand SaveFileCommand { get; }
    public ICommand ComputeCommand { get; }

    private CancellationTokenSource _cts;

    public MainWindowViewModel()
    {
        Dataset = new Dataset();
        LoadFileCommand = new RelayCommand(async () => await LoadFileAsync());
        SaveFileCommand = new RelayCommand(async () => await SaveFileAsync());
        ComputeCommand = new RelayCommand(async () => await ComputeAsync());
        SelectedMethod = Methods[0];
        SelectedLanguage = Languages[0];
    }

    private async Task LoadFileAsync()
    {
        var dialog = new OpenFileDialog();
        var result = await dialog.ShowAsync(new Window());

        if (result != null && result.Length > 0)
        {
            var loadedDataset = await FileManager.LoadFromFileAsync(result[0]);
            InputNumbers = string.Join(", ", loadedDataset.Values);
        }
    }

    private async Task SaveFileAsync()
    {
        if (Dataset == null || Dataset.Values.Count == 0) return;

        var dialog = new SaveFileDialog();
        var result = await dialog.ShowAsync(new Window());

        if (!string.IsNullOrEmpty(result))
        {
            await FileManager.SaveToFileAsync(Dataset, result);
        }
    }

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
                double result = await estimator.ComputeAsync(Dataset, progress, _cts.Token);
                Result = $"Result: {result:F2}";
            }
            catch (OperationCanceledException)
            {
                Result = "Calculation canceled.";
            }
        }
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
                                 .Select(s => double.TryParse(s, out var num) ? num : (double?)null)
                                 .Where(n => n.HasValue)
                                 .Select(n => n.Value)
                                 .ToList() ?? new List<double>();

        Dataset.SetValues(values);
    }
}
