using CommunityToolkit.Mvvm.ComponentModel;
using LLMChatTool.Classes;
using System.Windows;

namespace LLMChatTool.ViewModels;

public class ChatMessageViewModel : ObservableRecipient
{
    private string _text;
    private KindOfMessage _kindOfMessage = KindOfMessage.Bot;
    private const string USER_BACKGROUND_COLOR = "#c9deff";
    private const string SYSTEM_BACKGROUND_COLOR = "#f2ffe0";
    private const string BOT_BACKGROUND_COLOR = "#bfffdd";

    public string Text
    {
        get { return _text; }
        set
        {
            SetProperty(ref _text, value);
        }
    }

    public KindOfMessage KindOfMessage
    {
        get { return _kindOfMessage; }
        set
        {
            SetProperty(ref _kindOfMessage, value);
        }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get 
        {
            if (_kindOfMessage == KindOfMessage.User) return HorizontalAlignment.Right;
            if (_kindOfMessage == KindOfMessage.System) return HorizontalAlignment.Center;
            if (_kindOfMessage == KindOfMessage.Bot) return HorizontalAlignment.Left;           
            return HorizontalAlignment.Stretch;
        }
    }

    public string BackgroundColor
    {
        get {
            if (_kindOfMessage == KindOfMessage.User) return USER_BACKGROUND_COLOR;
            if (_kindOfMessage == KindOfMessage.System) return SYSTEM_BACKGROUND_COLOR;
            return BOT_BACKGROUND_COLOR;
        }
    }
}
