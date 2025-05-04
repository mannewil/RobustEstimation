using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.ComponentModel;

public class Dataset : INotifyPropertyChanged
{
    public ObservableCollection<double> Values { get; private set; } = new();

    public ObservableCollection<(double X, double Y)> Points { get; private set; } = new();

    public void SetValues(IEnumerable<double> vals)
    {
        Points.Clear();
        Values.Clear();
        foreach (var v in vals) Values.Add(v);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
    }

    public void SetPoints(IEnumerable<(double X, double Y)> pts)
    {
        Values.Clear();
        Points.Clear();
        foreach (var p in pts) Points.Add(p);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Points)));
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
