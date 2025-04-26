using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Avalonia;
using OxyPlot.Series;
using OxyPlot.Annotations;
using CommunityToolkit.Mvvm.ComponentModel;
using LinearAxis = OxyPlot.Axes.LinearAxis;

namespace RobustEstimation.ViewModels
{
    public partial class GraphWindowViewModel : ViewModelBase
    {
        private readonly Window _owner;
        private readonly PlotModel _model;

        [ObservableProperty] private PlotModel plotModel;
        public IRelayCommand SaveImageCommand { get; }

        public GraphWindowViewModel(IGraphable graphable)
        {            

            // --- Построение модели ---
            _model = new PlotModel
            {
                Title = graphable.GetGraphTitle(),
                Background = OxyColors.White,
                PlotAreaBackground = OxyColors.White,
                TextColor = OxyColors.Black,
                TitleColor = OxyColors.Black,
                PlotAreaBorderColor = OxyColors.Gray,
            };

            // Оси
            _model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "X",
                TextColor = OxyColors.Black,
                TitleColor = OxyColors.Black,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray
            });
            _model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Y",
                TextColor = OxyColors.Black,
                TitleColor = OxyColors.Black,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray
            });

            // Серии и аннотации
            foreach (var s in graphable.GetSeries())
                _model.Series.Add(s);
            foreach (var a in graphable.GetAnnotations())
                _model.Annotations.Add(a);

            PlotModel = _model;

            // Команда сохранения через SaveFileDialog
            SaveImageCommand = new RelayCommand(async () => await SaveImageAsync());
        }

        private async Task SaveImageAsync()
        {
            // 1) Создаём и настраиваем SaveFileDialog
            var dlg = new SaveFileDialog
            {
                Title = "Save chart as PNG",
                DefaultExtension = "png",
                InitialFileName = "chart.png",
                Filters = { new FileDialogFilter { Name = "PNG Image", Extensions = { "png" } } }
            };

            // 2) Показываем диалог и ждём путь
            var path = await dlg.ShowAsync(new Window());
            if (string.IsNullOrEmpty(path))
                return;

            // 3) Экспорт в PNG
            using var stream = File.Create(path);
            var exporter = new PngExporter
            {
                Width = 800,
                Height = 600,
                Background = OxyColors.White
            };
            exporter.Export(_model, stream);
        }
    }
}
