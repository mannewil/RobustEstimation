using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class HuberEstimator : RobustEstimatorBase
    {
        private readonly double delta;
        public List<double> ProcessedValues { get; private set; } = new();
        public double[,] CovarianceMatrix { get; private set; }

        public HuberEstimator(double delta = 1.5)
        {
            this.delta = delta;
        }

        public override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                if (data.Values == null || data.Values.Count == 0)
                    throw new ArgumentException("Dataset cannot be empty.");

                double median = MedianEstimator.ComputeMedian(data.Values);
                Console.WriteLine($"Median: {median}");

                ProcessedValues.Clear();
                List<double> weights = new();
                double weightedSum = 0, weightSum = 0;
                int count = data.Values.Count, processed = 0;

                foreach (var x in data.Values)
                {
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

                    double r = x - median;
                    double weight = Math.Abs(r) <= delta ? 1 : delta / Math.Abs(r);
                    weights.Add(weight);

                    double adjustedValue = median + weight * r;
                    ProcessedValues.Add(adjustedValue);

                    weightedSum += weight * adjustedValue;
                    weightSum += weight;

                    Console.WriteLine($"x: {x}, r: {r}, weight: {weight}, adjustedValue: {adjustedValue}");

                    processed++;
                    progress?.Report((processed * 100) / count);
                }

                double huberMean = weightedSum / weightSum;
                Console.WriteLine($"Huber Mean: {huberMean}");

                ComputeCovarianceMatrix(data.Values.ToList(), weights, huberMean);
                return huberMean;
            }, cancellationToken);
        }

        private void ComputeCovarianceMatrix(List<double> data, List<double> weights, double huberMean)
        {
            double weightSum = weights.Sum();
            double varianceSum = 0;

            for (int i = 0; i < data.Count; i++)
            {
                double term = weights[i] * Math.Pow(data[i] - huberMean, 2);
                varianceSum += term;
            }

            double variance = varianceSum / weightSum;
            CovarianceMatrix = new double[,] { { variance } };

            Console.WriteLine($"Covariance Matrix: {variance}");
        }
    }
}
