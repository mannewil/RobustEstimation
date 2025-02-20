using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    // Оценка Хьюбера (ограничивает влияние выбросов)
    public class HuberEstimator : RobustEstimatorBase
    {
        private readonly double threshold;

        public HuberEstimator(double threshold = 1.5)
        {
            this.threshold = threshold;
        }

        public override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                double mean = data.Values.Average();
                double sum = 0;
                int count = data.Values.Count;
                int processed = 0;

                foreach (var x in data.Values)
                {
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();
                    sum += Math.Abs(x - mean) <= threshold ? x : threshold * Math.Sign(x - mean);
                    processed++;
                    progress?.Report((processed * 100) / count);
                }
                return sum / count;
            }, cancellationToken);
        }
    }

}
