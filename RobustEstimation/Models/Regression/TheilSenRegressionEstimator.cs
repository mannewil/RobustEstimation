using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models.Regression;

public class TheilSenRegressionEstimator : RegressionEstimatorBase
{
    public List<double> ProcessedSlopes { get; private set; } = new();

    protected override async Task<(double Slope, double Intercept, double MedianSquaredResidual)>
        ComputeCoreAsync((double x, double y)[] pts, IProgress<int> progress, CancellationToken cancellation)
    {
        return await Task.Run(() =>
        {
            int n = pts.Length;
            int total = n * (n - 1) / 2, done = 0;
            var slopes = new List<double>(total);

            // 1) Collect all pairwise slopes
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    slopes.Add((pts[j].y - pts[i].y) / (pts[j].x - pts[i].x));
                    progress?.Report(++done * 100 / total);
                }
                   
            slopes.Sort();
            ProcessedSlopes.Clear();
            ProcessedSlopes.AddRange(slopes);
            double slope = slopes[slopes.Count / 2];

            // 2) Compute intercepts and take median
            var intercepts = pts.Select(p => p.y - slope * p.x).ToArray();
            Array.Sort(intercepts);
            double intercept = intercepts[intercepts.Length / 2];

            // 3) Median squared residual
            var sq = pts
                .Select(p => Math.Pow(p.y - (slope * p.x + intercept), 2))
                .OrderBy(v => v)
                .ToArray();
            double medSq = sq[sq.Length / 2];

            return (slope, intercept, medSq);
        }, cancellation);
    }
}
