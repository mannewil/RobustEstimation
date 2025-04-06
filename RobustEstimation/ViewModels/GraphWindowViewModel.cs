using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RobustEstimation.ViewModels;

public partial class GraphWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private PlotModel plotModel;

    public GraphWindowViewModel(IGraphable graphable)
    {
        var model = new PlotModel
        {
            Title = graphable.GetGraphTitle(),
            TextColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.White,
            TitleColor = OxyColors.White,
            Background = OxyColors.Black,
            // Увеличенные отступы, чтобы надписи не обрезались
            PlotMargins = new OxyThickness(60, 10, 100, 50)
        };

        // Добавление серий
        foreach (var series in graphable.GetSeries())
        {
            if (series is ScatterSeries scatter && scatter.MarkerFill.IsInvisible())
                scatter.MarkerFill = OxyColors.LightGreen;

            model.Series.Add(series);
        }

        // Добавление горизонтальной линии, если есть значение
        if (graphable.GetHorizontalLineValue() is double y)
        {
            var line = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = y,
                Color = OxyColors.Red,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 2,
                Text = graphable.GetLineLabel() ?? "",
                TextHorizontalAlignment = HorizontalAlignment.Right,
                TextVerticalAlignment = VerticalAlignment.Top,
                TextMargin = 10,
                TextColor = OxyColors.White
            };

            model.Annotations.Add(line);
        }

        // Добавление дополнительных аннотаций (если есть)
        foreach (var annotation in graphable.GetAnnotations())
        {
            if (annotation is LineAnnotation line)
            {
                line.TextColor = OxyColors.White;
                line.Color = OxyColors.Red;
            }

            model.Annotations.Add(annotation);
        }

        PlotModel = model;
        OnPropertyChanged(nameof(PlotModel));
    }
}
