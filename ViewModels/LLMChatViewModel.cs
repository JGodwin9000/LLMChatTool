using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LLMChatTool.Classes;
using LLMChatTool.Models;
using LLMChatTool.Models.Messages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace LLMChatTool.ViewModels;

public class LLMChatViewModel : ObservableRecipient, IDisposable
{
    public const string STARTUP_MESSAGE = "Chat started";
    private readonly string MODEL_FOLDER_PATH = Path.Combine(AppContext.BaseDirectory, LANGUAGE_MODEL_DIRECTORY);
    private const string LANGUAGE_MODEL_DIRECTORY = "GGUF_MODELS";
    private const string NO_MODEL_SELECTED_ERROR = "Unable to start. No model file has been selected.";

    private ChatMessageViewModel _currentBotMessage = null;
    private bool _isDirty = false;
    private Visibility _applyChangesLabelVisibility = Visibility.Hidden;
    private string _inputText = string.Empty;
    private FileSystemInfo _selectedModelFileInfo = null;
    private ObservableCollection<ChatMessageViewModel> _chatMessageCollection = new ObservableCollection<ChatMessageViewModel>();
    private List<FileSystemInfo> _llmFileInfos = null;
    private ChatBotLlamaSharp _llamaBot = new ChatBotLlamaSharp();
    private SamplingPipelineModel _samplingPipelineModel = new SamplingPipelineModel();

    public ICommand MainInputCommand => new RelayCommand(ProcessMainInput);
    public ICommand SendInputCommand => new RelayCommand(ProcessMainInput);
    public ICommand ClearChatComand => new RelayCommand(ClearChat);
    public ICommand ApplyChangesCommand => new RelayCommand(Start);
    public ICommand OpenModelsFolderCommand => new RelayCommand(OpenModelsFolder);
    public ICommand OpenHugginFaceCommand => new RelayCommand(OpenHuggingFace);
    public ICommand KillBotCommand => new RelayCommand(KillRunningBot);

    public SamplingPipelineModel SamplingPipelineModel
    {
        get
        {
            return _samplingPipelineModel;
        }

        set
        {
            _samplingPipelineModel = value;
        }
    }

    public ObservableCollection<ChatMessageViewModel> ChatMessages
    {
        get
        {
            return _chatMessageCollection;
        }
    }

    public bool IsDirty
    {
        get
        {
            return _isDirty;
        }

        set
        {
            SetProperty(ref _isDirty, value);
            if (_isDirty)
            {
                ApplyChangesLabelVisibility = Visibility.Visible;
            }
            else
            {
                ApplyChangesLabelVisibility = Visibility.Hidden;
            }
        }
    }

    public bool IsRunning
    {
        get
        {
            return _llamaBot.IsRunning;
        }
    }

    public Visibility ApplyChangesLabelVisibility
    {
        get
        {
            return _applyChangesLabelVisibility;
        }

        set
        {
            SetProperty(ref _applyChangesLabelVisibility, value);
        }
    }

    public string InputText
    {
        get
        {
            return _inputText;
        }

        set
        {
            SetProperty(ref _inputText, value);
        }
    }

    public FileSystemInfo SelectedModelFileInfo
    {
        get
        {
            return _selectedModelFileInfo;
        }

        set
        {
            IsDirty = true;
            SetProperty(ref _selectedModelFileInfo, value);
        }
    }

    public List<FileSystemInfo> ModelFileInfos
    {
        get
        {
            return _llmFileInfos;
        }

        set
        {
            SetProperty(ref _llmFileInfos, value);
        }
    }

    public LLMChatViewModel()
    {
        Initialize();
    }

    private void ClearChat()
    {
        if (_chatMessageCollection != null)
        {
            _chatMessageCollection.Clear();
        }
    }

    private void Initialize()
    {

        Messenger.Register<LLMChatViewModel, TextBoxEnterKeyPressedMessage, int>(this, 1, (r, msg) =>
        {
            InputText = msg.Text;
            ProcessMainInput();
        });

        Messenger.Register<LLMChatViewModel, SystemOutputMessage, int>(this, 1, (r, msg) =>
        {
            OutputSystemMessage(msg.Text);
        });

        Messenger.Register<LLMChatViewModel, EndBotMessage, int>(this, 1, (r, msg) =>
        {
            if (_currentBotMessage != null && _currentBotMessage.Text.EndsWith("User:"))
            {
                _currentBotMessage.Text = _currentBotMessage.Text.Replace("User:", string.Empty);
            }
            _currentBotMessage = null;
            ScrollToBottom();
        });

        Messenger.Register<LLMChatViewModel, PartialBotOutputMessage, int>(this, 1, (r, msg) =>
        {
            if (_currentBotMessage == null)
            {
                _currentBotMessage = new ChatMessageViewModel() { KindOfMessage = KindOfMessage.Bot, Text = msg.Text };
                _chatMessageCollection.Add(_currentBotMessage);
            }

            _currentBotMessage.Text += msg.Text;
            if (_currentBotMessage.Text.StartsWith("AssistantAssistant:"))
            {
                _currentBotMessage.Text = _currentBotMessage.Text.Replace("AssistantAssistant:", "Assistant:"); //hack. don't hate me
            }

            ScrollToBottom();
        });

        Messenger.Register<LLMChatViewModel, PropertyGridValueChangeMessage, int>(this, 1, (r, msg) =>
        {
             IsDirty = true;           
        });

        if (Directory.Exists(MODEL_FOLDER_PATH))
        {
            var dirInfo = new DirectoryInfo(MODEL_FOLDER_PATH);
            ModelFileInfos = dirInfo.GetFileSystemInfos().ToList();
        }

        if (!Directory.Exists(MODEL_FOLDER_PATH))
        {
            Directory.CreateDirectory(MODEL_FOLDER_PATH);
        }
    }

    private void KillRunningBot()
    {
        IsDirty = true;
        _llamaBot.KillBot();
    }

    private void Start()
    {
        if (_selectedModelFileInfo == null)
        {
            OutputAppMessage(NO_MODEL_SELECTED_ERROR);
            OutputSystemMessage(NO_MODEL_SELECTED_ERROR);
            return;
        }

        IsDirty = false;

        if (_llamaBot != null)
        {
            _llamaBot.Dispose();
            _llamaBot = new ChatBotLlamaSharp();
        }

        OutputSystemMessage($"{STARTUP_MESSAGE}\n\nSeclectedModel: {_selectedModelFileInfo.Name}");

        _llamaBot.ModelFullName = _selectedModelFileInfo.FullName;
        try
        {
            _llamaBot.Start(_samplingPipelineModel.ToDefaultSamplingPipeline());
        }
        catch (Exception ex)
        {
            OutputAppMessage("An error occured while starting the bot.");
            OutputAppMessage($"{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }

        ScrollToBottom();
    }

    private void OutputSystemMessage(string text)
    {
        _chatMessageCollection.Add(new ChatMessageViewModel() { KindOfMessage = KindOfMessage.System, Text = text });
        ScrollToBottom();
    }

    private void OutputAppMessage(string text)
    {
        Messenger.Send(new AppOutputMessage() { Text = text }, 1);
    }

    private void OpenHuggingFace()
    {
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "https://huggingface.co/models?library=gguf&sort=trending",
            UseShellExecute = true
        });
    }

    private void OpenModelsFolder()
    {
        if (!Directory.Exists(MODEL_FOLDER_PATH))
        {
            OutputSystemMessage($"The models folder ({MODEL_FOLDER_PATH}) does not exist.");
            return;
        }

        using Process process = Process.Start("explorer.exe", MODEL_FOLDER_PATH);
    }

    private void ProcessMainInput()
    {
        if (string.IsNullOrWhiteSpace(InputText)) { return; }
        if (!IsRunning) { return; }

        if (_selectedModelFileInfo == null)
        {
            OutputSystemMessage($@"Please select a model from the drop down. 
If there are none listed, download one or more LLM models and place them 
in a folder in the bin folder named {LANGUAGE_MODEL_DIRECTORY} and restart the app. 
If that folder doesn't exist you will need to create it.");
            return;
        }

        string inputText = InputText;
        InputText = string.Empty;
        _chatMessageCollection.Add(new ChatMessageViewModel() { KindOfMessage = KindOfMessage.User, Text = inputText });
        _currentBotMessage = null;
        ScrollToBottom();

        _llamaBot.ProcessInput(inputText);
    }

    private void ScrollToBottom()
    {
        Messenger.Send(new ScrollToBottomMessage(), 1);
    }

    public void Dispose()
    {
        if (_llamaBot != null) _llamaBot.Dispose();
    }
}
