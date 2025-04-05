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
            TextColor = OxyColors.Black,
            PlotAreaBorderColor = OxyColors.Black,
            //LegendTextColor = OxyColors.White,
            TitleColor = OxyColors.Black,
            Background = OxyColors.White,
        };

        foreach (var series in graphable.GetSeries())
        {
            // Установим цвет точек, если они ещё не настроены
            if (series is ScatterSeries scatter)
            {
                scatter.MarkerFill = scatter.MarkerFill.IsInvisible() ? OxyColors.LightGray : scatter.MarkerFill;
            }

            model.Series.Add(series);
        }

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
