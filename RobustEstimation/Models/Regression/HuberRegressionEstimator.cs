using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models.Regression;

public class HuberRegressionEstimator : RegressionEstimatorBase
{
    private readonly double _delta;
    public HuberRegressionEstimator(double delta = 1.345) => _delta = delta;

    protected override async Task<(double Slope, double Intercept, double MedianSquaredResidual)>
        ComputeCoreAsync((double x, double y)[] pts, IProgress<int> progress, CancellationToken cancellation)
    {
        return await Task.Run(() =>
        {
            int n = pts.Length;
            // 1) Initial guess via OLS
            double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;
            foreach (var p in pts)
            {
                sumX += p.x; sumY += p.y;
                sumXY += p.x * p.y; sumXX += p.x * p.x;
            }
            double slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            // 2) IRLS iterations
            for (int iter = 0; iter < 20; iter++)
            {
                cancellation.ThrowIfCancellationRequested();
                var w = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double r = pts[i].y - (slope * pts[i].x + intercept);
                    double abs = Math.Abs(r);
                    w[i] = abs <= _delta ? 1 : _delta / abs;
                }
                // Weighted least squares
                double sw = 0, swx = 0, swy = 0, swxx = 0, swxy = 0;
                for (int i = 0; i < n; i++)
                {
                    double wi = w[i];
                    sw += wi;
                    swx += wi * pts[i].x;
                    swy += wi * pts[i].y;
                    swxx += wi * pts[i].x * pts[i].x;
                    swxy += wi * pts[i].x * pts[i].y;
                }
                slope = (sw * swxy - swx * swy) / (sw * swxx - swx * swx);
                intercept = (swy - slope * swx) / sw;
                progress?.Report(iter * 100 / 20);
            }

            // Final median squared residual
            var sq = pts
                .Select(p => Math.Pow(p.y - (slope * p.x + intercept), 2))
                .OrderBy(v => v)
                .ToArray();
            double medSq = sq[sq.Length / 2];

            return (slope, intercept, medSq);
        }, cancellation);
    }
}
