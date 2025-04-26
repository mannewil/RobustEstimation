using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models.Regression;

public abstract class RegressionEstimatorBase
{
    public async Task<RegressionResult> FitAsync(
    IEnumerable<(double x, double y)> points,
    IProgress<int> progress = null,
    CancellationToken cancellation = default)
    {
        var pts = points.ToArray();
        if (pts.Length < 2)
            throw new ArgumentException("At least two points are required", nameof(points));

        var sw = Stopwatch.StartNew();
        var core = await ComputeCoreAsync(pts, progress, cancellation);
        sw.Stop();

        // 1) Compute SST (total sum of squares)
        double meanY = pts.Average(p => p.y);
        double ssTot = pts.Sum(p => (p.y - meanY) * (p.y - meanY));

        // 2) Compute SSR (residual sum of squares)
        double ssRes = pts.Sum(p =>
        {
            double pred = core.Slope * p.x + core.Intercept;
            return (p.y - pred) * (p.y - pred);
        });

        double r2 = ssTot > 0
           ? 1.0 - ssRes / ssTot
           : 1.0;  // если все Y одинаковы

        return new RegressionResult
        {
            Slope = core.Slope,
            Intercept = core.Intercept,
            MedianSquaredResidual = core.MedianSquaredResidual,
            Elapsed = sw.Elapsed,
            RSquared = r2
        };
    }

    /// <summary>
    /// Child classes implement this: compute slope, intercept and median squared residual.
    /// Points array has length >= 2; progress.Report should go 0..100.
    /// </summary>
    protected abstract Task<(double Slope, double Intercept, double MedianSquaredResidual)>
        ComputeCoreAsync(
            (double x, double y)[] pts,
            IProgress<int> progress,
            CancellationToken cancellation);
}
