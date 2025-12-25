using System.Windows;
using ThreeMess.ViewModels;

namespace ThreeMess;

public partial class AddFriendWindow : Window
{
    public AddFriendWindow()
    {
        InitializeComponent();

        var vm = new AddFriendViewModel();
        vm.RequestClose += () => Close();
        DataContext = vm;

        Loaded += (_, _) => vm.OnLoaded();
    }
}
