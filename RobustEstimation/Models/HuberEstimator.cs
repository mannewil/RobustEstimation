// RobustEstimation.Models/HuberEstimator.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class HuberEstimator : RobustEstimatorBase
    {
        private readonly double _delta;
        public List<double> ProcessedValues { get; private set; } = new();
        public double[,] CovarianceMatrix { get; private set; }

        public HuberEstimator(double delta = 1.5)
        {
            _delta = delta;
        }

        protected override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var values = data.Values;
                double median = MedianEstimator.ComputeMedian(values);

                var weights = new List<double>(values.Count);
                var adjusted = new List<double>(values.Count);

                double wSum = 0, wMeanSum = 0;
                int i = 0;

                foreach (var x in values)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    double r = x - median;
                    double w = Math.Abs(r) <= _delta
                        ? 1
                        : _delta / Math.Abs(r);
                    weights.Add(w);

                    double adj = median + w * r;
                    adjusted.Add(adj);

                    wSum += w;
                    wMeanSum += w * adj;

                    progress?.Report(++i * 100 / values.Count);
                }

                // Huber-mean
                double huberMean = wMeanSum / wSum;
                ProcessedValues = adjusted;

                ComputeCovarianceMatrix(values.ToList(), weights, huberMean);
                return huberMean;
            }, cancellationToken);
        }

        private void ComputeCovarianceMatrix(List<double> data, List<double> weights, double mean)
        {
            int n = data.Count;
            if (n < 2)
            {
                CovarianceMatrix = new double[,] { { 0 } };
                return;
            }

            // Weighted sum of squares
            double wSum = weights.Sum();
            double wSqSum = weights.Sum(w => w * w);
            double num = 0;
            for (int i = 0; i < n; i++)
                num += weights[i] * Math.Pow(data[i] - mean, 2);

            // Weighted sample variance (unbiased)
            double denom = wSum - wSqSum / wSum;
            double variance = denom > 0
                ? num / denom
                : num / wSum;

            CovarianceMatrix = new double[,] { { variance } };
        }
    }
}
