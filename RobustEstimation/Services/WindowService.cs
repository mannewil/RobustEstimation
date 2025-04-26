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

        // создаём окно
        var graphWindow = new GraphWindow();
        // передаём и IGraphable, и само окно в VM
        var vm = new GraphWindowViewModel(graphable);
        graphWindow.DataContext = vm;
        graphWindow.Show();
    }

}
