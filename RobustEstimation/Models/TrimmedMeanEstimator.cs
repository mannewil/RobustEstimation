// RobustEstimation.Models/TrimmedMeanEstimator.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class TrimmedMeanEstimator : RobustEstimatorBase
    {
        private double _trimPercentage;
        public List<double> ProcessedData { get; private set; } = new();
        public double[,] CovarianceMatrix { get; private set; }

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

        protected override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var sorted = data.Values.OrderBy(v => v).ToList();
                int trimCount = (int)(sorted.Count * TrimPercentage);
                ProcessedData = sorted
                    .Skip(trimCount)
                    .Take(sorted.Count - 2 * trimCount)
                    .ToList();

                double mean = ProcessedData.Average();

                ComputeCovarianceMatrix(ProcessedData, mean);
                progress?.Report(100);
                return mean;
            }, cancellationToken);
        }

        private void ComputeCovarianceMatrix(List<double> data, double mean)
        {
            int n = data.Count;
            if (n < 2)
            {
                CovarianceMatrix = new double[,] { { 0 } };
                return;
            }

            // Несмещённая оценка дисперсии
            double sumSq = data.Sum(v => Math.Pow(v - mean, 2));
            double variance = sumSq / (n - 1);

            CovarianceMatrix = new double[,] { { variance } };
        }
    }
}
