using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;

namespace RobustEstimation.Models
{
    public class Dataset : INotifyPropertyChanged
    {
        private ObservableCollection<double> _values = new();

        public ObservableCollection<double> Values
        {
            get => _values;
            private set
            {
                _values = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
            }
        }

        public void SetValues(IEnumerable<double> newValues)
        {
            _values.Clear();
            foreach (var value in newValues)
            {
                _values.Add(value);
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Values)));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
