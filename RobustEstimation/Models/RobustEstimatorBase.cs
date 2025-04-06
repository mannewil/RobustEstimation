using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobustEstimation.Models;

public abstract class RobustEstimatorBase
{
    public async Task<(double result, TimeSpan duration)> ComputeWithTimingAsync(Dataset dataset, IProgress<int> progress, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        double result = await ComputeAsync(dataset, progress, cancellationToken);
        stopwatch.Stop();
        return (result, stopwatch.Elapsed);
    }

    protected abstract Task<double> ComputeAsync(Dataset dataset, IProgress<int> progress, CancellationToken cancellationToken);
}

