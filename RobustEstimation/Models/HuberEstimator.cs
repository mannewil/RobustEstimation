using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class HuberEstimator : RobustEstimatorBase
    {
        private readonly double threshold;
        public List<double> ProcessedValues { get; private set; } = new();

        public HuberEstimator(double threshold = 1.5)
        {
            this.threshold = threshold;
        }

        public override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                double median = MedianEstimator.ComputeMedian(data.Values); // ✅ Используем готовый метод
                ProcessedValues.Clear();
                double sum = 0;
                int count = data.Values.Count;
                int processed = 0;

                foreach (var x in data.Values)
                {
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

                    double adjustedValue = Math.Abs(x - median) <= threshold ? x : median + threshold * Math.Sign(x - median);
                    sum += adjustedValue;
                    ProcessedValues.Add(adjustedValue);

                    processed++;
                    progress?.Report((processed * 100) / count);
                }

                return sum / count;
            }, cancellationToken);
        }
    }
}
