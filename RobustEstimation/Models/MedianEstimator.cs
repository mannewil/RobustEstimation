using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class MedianEstimator : RobustEstimatorBase
    {
        public override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var sortedValues = data.Values.OrderBy(x => x).ToList();
                int n = sortedValues.Count;
                progress?.Report(100);
                return (n % 2 == 0) ? (sortedValues[n / 2 - 1] + sortedValues[n / 2]) / 2.0 : sortedValues[n / 2];
            }, cancellationToken);
        }
    }
}
