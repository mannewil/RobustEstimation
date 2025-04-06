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
                if (data.Values == null || data.Values.Count == 0)
                    throw new ArgumentException("Dataset cannot be empty.");

                var sortedValues = data.Values.ToList();
                sortedValues.Sort();

                int trimCount = (int)Math.Round(sortedValues.Count * TrimPercentage); // Округляем правильно
                ProcessedData = sortedValues.Skip(trimCount).Take(sortedValues.Count - 2 * trimCount).ToList();

                double trimmedMean = ProcessedData.Average();
                Console.WriteLine($"Trimmed Mean: {trimmedMean}");

                ComputeCovarianceMatrix(ProcessedData, trimmedMean);
                progress?.Report(100);
                return trimmedMean;
            }, cancellationToken);
        }

        private void ComputeCovarianceMatrix(List<double> data, double trimmedMean)
        {
            double varianceSum = 0;
            int count = data.Count;

            foreach (var value in data)
            {
                double deviation = value - trimmedMean;
                varianceSum += deviation * deviation;
            }

            double variance = count > 1 ? varianceSum / (count - 1) : 0; // Несмещённая оценка дисперсии
            CovarianceMatrix = new double[,] { { variance } };
            Console.WriteLine($"Covariance Matrix: {variance}");
        }
    }
}
