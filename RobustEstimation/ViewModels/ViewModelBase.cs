using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RobustEstimation.ViewModels;

public class ViewModelBase : ObservableObject
{
    protected string FormatMatrix(double[,] m)
    {
        var sb = new StringBuilder();
        int rows = m.GetLength(0), cols = m.GetLength(1);
        for (int i = 0; i < rows; i++)
        {
            sb.Append("[ ");
            for (int j = 0; j < cols; j++)
            {
                sb.Append(m[i, j].ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                if (j < cols - 1) sb.Append(", ");
            }
            sb.Append(" ]");
            if (i < rows - 1) sb.AppendLine();
        }
        return sb.ToString();
    }
}
