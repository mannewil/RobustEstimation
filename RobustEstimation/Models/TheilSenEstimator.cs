using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    public class TheilSenEstimator : RobustEstimatorBase
    {
        public List<double> ProcessedSlopes { get; private set; } = new();

        public override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var slopes = new List<double>();
                int count = data.Values.Count;
                int processed = 0, total = count * (count - 1) / 2;

                for (int i = 0; i < count - 1; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();
                        slopes.Add((data.Values[j] - data.Values[i]) / (j - i));
                        processed++;
                        progress?.Report((processed * 100) / total);
                    }
                }
                slopes.Sort();
                ProcessedSlopes = slopes; // Сохраняем наклоны
                return slopes[slopes.Count / 2];
            }, cancellationToken);
        }
    }
}
