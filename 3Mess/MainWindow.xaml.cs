using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ThreeMess.ViewModels;

namespace ThreeMess;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        Loaded += (_, _) =>
        {
            UpdateEmptyState();

            if (vm.Messages is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += (_, _) => ScrollToBottom();
            }

            if (vm is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.SelectedConversation))
                    {
                        UpdateEmptyState();
                    }
                };
            }

            DraftBox.PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Enter) return;
                if (vm.SendMessageCommand.CanExecute(null))
                {
                    vm.SendMessageCommand.Execute(null);
                }
                e.Handled = true;
            };
        };
    }

    private void UpdateEmptyState()
    {
        if (DataContext is not MainViewModel vm) return;
        EmptyState.Visibility = vm.SelectedConversation == null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ScrollToBottom()
    {
        if (DataContext is not MainViewModel vm) return;
        var last = vm.Messages.LastOrDefault();
        if (last == null) return;
        Dispatcher.BeginInvoke(() => MessagesList.ScrollIntoView(last));
    }
}
