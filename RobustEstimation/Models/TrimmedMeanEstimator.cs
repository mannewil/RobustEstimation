using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RobustEstimation.Models
{
    public class TrimmedMeanEstimator : RobustEstimatorBase
    {
        private double _trimPercentage;
        public List<double> ProcessedData { get; private set; } = new();

        public double TrimPercentage
        {
            get => _trimPercentage;
            set
            {
                if (value < 0 || value > 0.5)
                    throw new ArgumentOutOfRangeException(nameof(value), "Trim percentage must be between 0 and 0.5");
                _trimPercentage = value;
            }
        }

        public TrimmedMeanEstimator(double trimPercentage = 0.1)
        {
            TrimPercentage = trimPercentage;
        }

        public override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var sortedValues = data.Values.ToList();
                sortedValues.Sort();

                int trimCount = (int)(sortedValues.Count * TrimPercentage);
                ProcessedData = sortedValues.Skip(trimCount).Take(sortedValues.Count - 2 * trimCount).ToList();

                progress?.Report(100);
                return ProcessedData.Average();
            }, cancellationToken);
        }
    }
}
