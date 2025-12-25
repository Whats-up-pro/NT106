using System.Collections.Generic;
using System.Windows;
using ThreeMess.ViewModels;

namespace ThreeMess;

public partial class CreateGroupWindow : Window
{
    public string? CreatedConversationId { get; private set; }

    public CreateGroupWindow(IReadOnlyList<FriendItemViewModel> friends)
    {
        InitializeComponent();

        var vm = new CreateGroupViewModel(friends);
        vm.Created += conversationId =>
        {
            CreatedConversationId = conversationId;
            DialogResult = true;
            Close();
        };

        DataContext = vm;
        Loaded += (_, _) => vm.OnLoaded();
    }
}
