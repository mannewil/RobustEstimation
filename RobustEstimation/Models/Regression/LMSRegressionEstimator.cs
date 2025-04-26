using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models.Regression;

public class LMSRegressionEstimator : RegressionEstimatorBase
{
    public List<double> ProcessedResiduals { get; private set; } = new();

    protected override async Task<(double Slope, double Intercept, double MedianSquaredResidual)>
        ComputeCoreAsync((double x, double y)[] pts, IProgress<int> progress, CancellationToken cancellation)
    {
        return await Task.Run(() =>
        {
            int n = pts.Length;
            int total = n * (n - 1) / 2, done = 0;
            double bestMed = double.MaxValue, bestSlope = 0, bestIntercept = 0;

            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    double slope = (pts[j].y - pts[i].y) / (pts[j].x - pts[i].x);
                    double intercept = pts[i].y - slope * pts[i].x;

                    var sq = pts
                        .Select(p => Math.Pow(p.y - (slope * p.x + intercept), 2))
                        .OrderBy(v => v)
                        .ToArray();
                    double medSq = sq[sq.Length / 2];

                    if (medSq < bestMed)
                    {
                        bestMed = medSq;
                        bestSlope = slope;
                        bestIntercept = intercept;
                    }

                    progress?.Report(++done * 100 / total);

                    ProcessedResiduals = pts
                                   .Select(p => Math.Pow(p.y - (bestSlope * p.x + bestIntercept), 2))
                                .ToList();
                }

            return (bestSlope, bestIntercept, bestMed);
        }, cancellation);
    }
}
