using LLMChatTool.Classes;
using LLMChatTool.Models.Messages;
using LLMChatTool.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace LLMChatTool.Views;

public partial class LLMChatView : Window
{
    private const int MAX_OUTPUT_LINES = 1000; // Maximum number of lines to keep in the chat output.

    private TextBoxEnterKeyPressedMessage _enterMessage = new TextBoxEnterKeyPressedMessage();
    private readonly MessengerHelper _messengerHelper;  
    private List<string> _outputLines = new List<string>();

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

        this.ContentRendered -= LLMChatView_ContentRendered;
        this.ContentRendered += LLMChatView_ContentRendered;

        _messengerHelper.Messenger.Register<LLMChatViewModel, AppOutputMessage, int>((LLMChatViewModel)this.DataContext, 1, (r, outputMessage) =>
        {
            Output(outputMessage.Text);
        });

        Output("Application Loaded");
    }

    private void Output(string text)
    {
        _outputLines.Add($"{DateTime.Now.ToLongTimeString()} - {text}");
        if (_outputLines.Count > MAX_OUTPUT_LINES)
        {
            _outputLines.RemoveAt(0);
        }

        OutputRun.Text = String.Join(Environment.NewLine, _outputLines);
        RichTextBoxOutput.ScrollToEnd();
    }

    private void LLMChatView_ContentRendered(object? sender, EventArgs e)
    {
        var firsLoadStoryBoard = Resources["LoadingBGStoryboard"] as Storyboard;
        firsLoadStoryBoard?.Begin();
    }

    private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            _enterMessage.Text = ((TextBox)sender).Text;
            _messengerHelper.Messenger.Send(_enterMessage, 1);
        }
    }

    private void LlamaSharpPropetyGrid_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
    {
        _messengerHelper.Messenger.Send(new PropertyGridValueChangeMessage(), 1);
    }
}