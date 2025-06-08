using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;


namespace LLMChatTool.Classes;

public class MessengerHelper : ObservableRecipient
{
    public IMessenger Messenger
    {
        get
        {
            return base.Messenger;
        }
    }
}
