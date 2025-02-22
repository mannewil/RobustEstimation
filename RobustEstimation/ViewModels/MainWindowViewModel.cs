using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using RobustEstimation.Models;
using RobustEstimation.ViewModels.Methods;
using RobustEstimation.Views.Methods;

namespace RobustEstimation.ViewModels;

public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    public ObservableCollection<string> Methods { get; } = new()
    {
        "Median", "Huber", "Trimmed Mean", "Theil-Sen", "LMS"
    };

    public ObservableCollection<string> Languages { get; } = new()
    {
        "English", "Czech", "Russian"
    };

    private string _selectedMethod;
    public string SelectedMethod
    {
        get => _selectedMethod;
        set
        {
            if (SetProperty(ref _selectedMethod, value))
            {
                UpdateMethodView();
            }
        }
    }

    private string _selectedLanguage;
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set { _selectedLanguage = value; OnPropertyChanged(nameof(SelectedLanguage)); }
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(nameof(Progress)); }
    }

    private string _result;
    public string Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(nameof(Result)); }
    }

    private string _inputNumbers;
    public string InputNumbers
    {
        get => _inputNumbers;
        set
        {
            _inputNumbers = value;
            OnPropertyChanged(nameof(InputNumbers));
            UpdateDataset();
        }
    }

    private ViewModelBase _currentMethodViewModel;
    public ViewModelBase CurrentMethodViewModel
    {
        get => _currentMethodViewModel;
        set => SetProperty(ref _currentMethodViewModel, value);
    }

    private Dataset _dataset;
    public Dataset Dataset
    {
        get => _dataset;
        set { _dataset = value; OnPropertyChanged(nameof(Dataset)); }
    }

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

            // Обновляем строку чисел, чтобы они появились в текстбоксе
            InputNumbers = string.Join(", ", loadedDataset.Values);
        }
    }


    private async Task SaveFileAsync()
    {
        if (_dataset == null || _dataset.Values.Count == 0) return;

        var dialog = new SaveFileDialog();
        var result = await dialog.ShowAsync(new Window());

        if (!string.IsNullOrEmpty(result))
        {
            await FileManager.SaveToFileAsync(_dataset, result);
        }
    }



    private async Task ComputeAsync()
    {
        if (_dataset == null || _dataset.Values.Count == 0)
        {
            UpdateDataset(); // Используем введенные данные
        }

        if (_dataset.Values.Count == 0) return;

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
                double result = await estimator.ComputeAsync(_dataset, progress, _cts.Token);
                Result = $"Result: {result:F2}";
            }
            catch (OperationCanceledException)
            {
                Result = "Calculation canceled.";
            }
        }
    }

    private void UpdateMethodView()
    {
        CurrentMethodViewModel = SelectedMethod switch
        {
            "Median" => new MedianMethodViewModel(Dataset, this),
            //"Huber" => new HuberMethodView(),
            "Trimmed Mean" => new TrimmedMeanMethodViewModel(Dataset, this),
            //"Theil-Sen" => new TheilSenMethodView(),
            //"LMS" => new LMSMethodView(),
            _ => null
        };
        OnPropertyChanged(nameof(CurrentMethodViewModel));
    }

    private void UpdateDataset()
    {
        if (Dataset == null)
            Dataset = new Dataset(); // Предотвращение null-ссылки

        var values = InputNumbers?.Split(new[] { ' ', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => double.TryParse(s, out var num) ? num : (double?)null)
                                 .Where(n => n.HasValue)
                                 .Select(n => n.Value)
                                 .ToList() ?? new List<double>(); // Если InputNumbers = null, создаем пустой список

        Dataset.SetValues(values);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
