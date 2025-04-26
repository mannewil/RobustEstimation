using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobustEstimation.Models.Regression;

public class RegressionResult
{
    public double Slope { get; init; }
    public double Intercept { get; init; }
    public double MedianSquaredResidual { get; init; }
    public TimeSpan Elapsed { get; init; }
    public double RSquared { get; set; }
}
