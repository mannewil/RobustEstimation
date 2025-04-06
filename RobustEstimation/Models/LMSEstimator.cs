using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class LMSEstimator : RobustEstimatorBase
    {
        public List<double> ProcessedErrors { get; private set; } = new();
        public double[,] CovarianceMatrix { get; private set; }

        protected override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                if (data.Values == null || data.Values.Count == 0)
                    throw new ArgumentException("Dataset cannot be empty.");

                var squaredErrors = new List<double>();
                var median = new MedianEstimator().ComputeWithTimingAsync(data, progress, cancellationToken).Result;
                int processed = 0, count = data.Values.Count;

                foreach (var value in data.Values)
                {
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();
                    double error = Math.Pow(value - median.result, 2);
                    squaredErrors.Add(error);
                    processed++;
                    progress?.Report((processed * 100) / count);
                }

                squaredErrors.Sort();
                ProcessedErrors = squaredErrors;

                // Вычисляем LMS (медиану квадратов ошибок)
                int mid = squaredErrors.Count / 2;
                double lms = squaredErrors.Count % 2 == 0
                    ? (squaredErrors[mid - 1] + squaredErrors[mid]) / 2.0
                    : squaredErrors[mid];

                // Вычисляем ковариационную матрицу
                ComputeCovarianceMatrix(ProcessedErrors, lms);

                return lms;
            }, cancellationToken);
        }

        private void ComputeCovarianceMatrix(List<double> data, double lms)
        {
            if (data.Count < 2)
            {
                CovarianceMatrix = new double[,] { { 0 } };
                return;
            }

            double mean = data.Average(); // Среднее квадратичных ошибок
            double varianceSum = 0;
            int count = data.Count;

            foreach (var value in data)
            {
                double deviation = value - mean;  // Отклонение от среднего
                varianceSum += deviation * deviation;
            }

            double variance = varianceSum / (count - 1);  // Несмещённая дисперсия
            CovarianceMatrix = new double[,] { { variance } };
            Console.WriteLine($"Covariance matrix: {variance:F4}");  // Число в нормальном формате
        }

    }
}
