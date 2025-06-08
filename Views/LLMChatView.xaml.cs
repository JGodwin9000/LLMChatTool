using LLMChatTool.Classes;
using LLMChatTool.Models;
using LLMChatTool.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace LLMChatTool.Views;

public partial class LLMChatView : Window
{
    private TextBoxEnterKeyPressedMessage _enterMessage = new TextBoxEnterKeyPressedMessage();
    private readonly MessengerHelper _messengerHelper;   

    public LLMChatView()
    {
        InitializeComponent();
        _messengerHelper = new MessengerHelper();
        InitializeView();
    }

    private void LlamaSharpChatView_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (llamaBotVM != null)
        {
            llamaBotVM.Dispose();
        }
    }

    private void InitializeView()
    {
        this.Closing -= LlamaSharpChatView_Closing;
        this.Closing += LlamaSharpChatView_Closing;

        _messengerHelper.Messenger.Register<LLMChatViewModel, ScrollToBottomMessage, int>((LLMChatViewModel)this.DataContext, 1, (r, msg) =>
        {
            ChatListBox.Items.MoveCurrentToLast();
            ChatListBox.ScrollIntoView(ChatListBox.Items.CurrentItem);
        });

        this.Loaded -= LLMChatView_Loaded;
        this.Loaded += LLMChatView_Loaded;
        this.ContentRendered -= LLMChatView_ContentRendered;
        this.ContentRendered += LLMChatView_ContentRendered;
    }

    private void LLMChatView_ContentRendered(object? sender, EventArgs e)
    {
        
    }

    private void LLMChatView_Loaded(object sender, RoutedEventArgs e)
    {
        var sb = Resources["LoadingBGStoryboard"] as Storyboard;
        sb?.Begin();
    }

    private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            _enterMessage.Text = ((TextBox)sender).Text;
            _messengerHelper.Messenger.Send(_enterMessage, 1);
        }
    }
}