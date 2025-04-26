using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;

namespace RobustEstimation.Properties;

public class LocExtension : MarkupExtension
{
    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Resources
                   .ResourceManager
                   .GetString(Key, CultureInfo.CurrentUICulture)
               ?? $"!{Key}!";
    }
}
