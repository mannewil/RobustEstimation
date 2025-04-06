using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class MedianEstimator : RobustEstimatorBase
    {
        protected override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                double median = ComputeMedian(data.Values);
                progress?.Report(100);
                return median;
            }, cancellationToken);
        }

        public static double ComputeMedian(IEnumerable<double> values)
        {
            if (values == null || !values.Any()) throw new ArgumentException("Dataset is empty.");

            var sorted = values.OrderBy(x => x).ToList();
            int n = sorted.Count;
            return (n % 2 == 0) ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0 : sorted[n / 2];
        }
    }
}
