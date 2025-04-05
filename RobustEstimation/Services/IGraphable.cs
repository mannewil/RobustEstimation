using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

public interface IGraphable
{
    /// <summary>
    /// Возвращает список графических серий (LineSeries, ScatterSeries и т.п.), которые нужно отобразить.
    /// </summary>
    IEnumerable<Series> GetSeries();

    /// <summary>
    /// Возвращает список аннотаций (например, горизонтальных линий).
    /// </summary>
    IEnumerable<Annotation> GetAnnotations();

    /// <summary>
    /// Заголовок графика.
    /// </summary>
    string GetGraphTitle();
}
