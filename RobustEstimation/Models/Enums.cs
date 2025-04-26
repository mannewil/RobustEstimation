using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobustEstimation.Models;

public enum MethodType
{
    Median,
    Huber,
    TrimmedMean,
    TheilSen,
    LMS
}

public enum AppLanguage
{
    English,
    Czech,
    Russian
}
