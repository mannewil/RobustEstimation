using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RobustEstimation.Models
{
    public partial class Dataset : ObservableObject
    {
        /// <summary>
        /// Original values loaded into the dataset. These do not change after the initial load.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<double> originalValues = new();

        /// <summary>
        /// Current values in the dataset, which may be modified by calculations.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<double> values = new();

        /// <summary>
        /// Sets the dataset values. If original values are empty, they are initialized.
        /// </summary>
        public void SetValues(IEnumerable<double> newValues)
        {
            var valuesList = newValues.ToList();

            if (originalValues.Count == 0) // Store original values only once
            {
                OriginalValues = new ObservableCollection<double>(valuesList);
            }

            Values = new ObservableCollection<double>(valuesList);
        }

        /// <summary>
        /// Restores the dataset to its original values.
        /// </summary>
        public void ResetToOriginal()
        {
            Values = new ObservableCollection<double>(OriginalValues);
        }

        /// <summary>
        /// Checks if the dataset has been modified.
        /// </summary>
        public bool IsModified() => !Values.SequenceEqual(OriginalValues.ToList());      
    }
}
