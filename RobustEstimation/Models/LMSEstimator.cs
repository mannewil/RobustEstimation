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

        public override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var squaredErrors = new List<double>();
                double median = new MedianEstimator().ComputeAsync(data, progress, cancellationToken).Result;
                int processed = 0, count = data.Values.Count;

                foreach (var value in data.Values)
                {
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();
                    double error = Math.Pow(value - median, 2);
                    squaredErrors.Add(error);
                    processed++;
                    progress?.Report((processed * 100) / count);
                }
                squaredErrors.Sort();
                ProcessedErrors = squaredErrors;
                int mid = squaredErrors.Count / 2;
                if (squaredErrors.Count % 2 == 0)
                    return (squaredErrors[mid - 1] + squaredErrors[mid]) / 2.0;
                else
                    return squaredErrors[mid];
            }, cancellationToken);
        }
    }
}
