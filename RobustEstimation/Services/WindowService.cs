using Avalonia.Controls;
using RobustEstimation;
using RobustEstimation.ViewModels;
using RobustEstimation.Views;

public class WindowService : IWindowService
{
    public void ShowGraphWindow(ViewModelBase viewModel)
    {
        if (viewModel is not IGraphable graphable)
            return;

        var graphWindow = new GraphWindow
        {
            DataContext = new GraphWindowViewModel(graphable)
        };

        graphWindow.Show();
    }

}
