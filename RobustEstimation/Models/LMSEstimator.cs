using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class LMSEstimator : RobustEstimatorBase
    {
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
                    squaredErrors.Add(Math.Pow(value - median, 2));
                    processed++;
                    progress?.Report((processed * 100) / count);
                }
                squaredErrors.Sort();
                return squaredErrors[squaredErrors.Count / 2];
            }, cancellationToken);
        }
    }

}
