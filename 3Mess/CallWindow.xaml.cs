using System.Windows;
using ThreeMess.ViewModels;

namespace ThreeMess;

public partial class CallWindow : Window
{
    public CallWindow(CallViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.CallEnded += () =>
        {
            try
            {
                Dispatcher.Invoke(() => Close());
            }
            catch { }
        };

        // Adjust grid columns/rows based on participant count
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CallViewModel.CallParticipants))
            {
                UpdateParticipantsGridLayout();
            }
        };

        Loaded += (_, _) =>
        {
            UpdateParticipantsGridLayout();
        };
    }

    private void UpdateParticipantsGridLayout()
    {
        try
        {
            var vm = DataContext as CallViewModel;
            if (vm == null) return;

            int count = vm.CallParticipants.Count;
            if (count <= 0) return;

            // The UniformGrid in the ItemsPanel will automatically adjust its layout
            // based on the number of items, so no manual adjustment is needed
        }
        catch { }
    }
}
