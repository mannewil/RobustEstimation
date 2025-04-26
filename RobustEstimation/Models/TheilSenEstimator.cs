using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models
{
    /// <summary>
    /// Computes the Theil–Sen slope estimator over a simple sequence of values,
    /// treating the index as the x‑coordinate and the value as y‑coordinate.
    /// </summary>
    public class TheilSenEstimator : RobustEstimatorBase
    {
        /// <summary>
        /// All pairwise slopes computed during the last call.
        /// </summary>
        public List<double> ProcessedSlopes { get; private set; } = new();

        /// <inheritdoc/>
        protected override async Task<double> ComputeAsync(Dataset data, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                // Copy to array for faster access
                var values = data.Values.ToArray();
                int n = values.Length;
                if (n < 2)
                    throw new InvalidOperationException("At least two values are required to compute Theil–Sen estimator.");

                int total = n * (n - 1) / 2;
                int done = 0;
                var slopes = new List<double>(total);

                // Compute all pairwise slopes: (y_j - y_i) / (j - i)
                for (int i = 0; i < n - 1; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        double slope = (values[j] - values[i]) / (j - i);
                        slopes.Add(slope);

                        // report progress in percent
                        progress?.Report(++done * 100 / total);
                    }
                }

                // Sort and remember
                slopes.Sort();
                ProcessedSlopes.Clear();
                ProcessedSlopes.AddRange(slopes);

                // Return the median slope
                return slopes[slopes.Count / 2];
            }, cancellationToken);
        }
    }
}
