using CommunityToolkit.Mvvm.ComponentModel;
using LLama;
using LLama.Common;
using LLama.Sampling;
using LLMChatTool.Models.Messages;
using System.IO;

namespace LLMChatTool.Classes;

public class ChatBotLlamaSharp : ObservableRecipient, IDisposable
{
    private const int CONTEXT_SIZE = 1024; // The longest length of chat as memory.
    private const int GPU_LAYER_COUNT = 90; // How many layers to offload to GPU. Please adjust it according to your GPU memory.
    private const int MAX_TOKENS = 1048; // Number of tokens that should appear in the answer. Remove it if antiprompt is enough for control.

    private ModelParams _parameters = null;
    private LLamaWeights _model = null;
    private LLamaContext _context = null;
    private InteractiveExecutor _executor = null;
    private ChatHistory _chatHistory = null;
    private ChatSession _session = null;
    private InferenceParams _inferenceParams = null;
    private string _modelFullName = string.Empty;
    private bool _isRunning  = false;
    private CancellationTokenSource _chatCancelToken;

    private EndBotMessage _endBotMessageModel = new EndBotMessage();
    private PartialBotOutputMessage _partialBotOutputModel = new PartialBotOutputMessage();
    private SystemOutputMessage _systemOutputModel = new SystemOutputMessage();
    private AppOutputMessage _appOutputModel = new AppOutputMessage();

    public bool IsRunning
    {
        get
        {
            return _isRunning;
        }
    }

    public string ModelFullName
    {
        get
        {
            return _modelFullName;
        }

        set
        {
            _modelFullName = value;
        }
    }

    public void Start(DefaultSamplingPipeline samplingPipeline)
    {
        _isRunning = false;

        if (string.IsNullOrWhiteSpace(_modelFullName))
        {
            OutputSystemMessage("Please select a model.");
            OutputAppMessage("Please select a model.");
            return;
        }

        if (!File.Exists(_modelFullName))
        {
            OutputSystemMessage($"Selected model file does not exist. {_modelFullName}");
            OutputAppMessage($"Selected model file does not exist. {_modelFullName}");
            return;
        }

        try
        {
            Dispose();

            _chatCancelToken = new CancellationTokenSource();

            _parameters = new ModelParams(_modelFullName)
            {
                ContextSize = CONTEXT_SIZE,
                GpuLayerCount = GPU_LAYER_COUNT
            };

            var modelFileInfo = new FileInfo(_modelFullName);

            OutputAppMessage($"Loading model file: {modelFileInfo.Name}");
            _model = LLamaWeights.LoadFromFile(_parameters);
            OutputAppMessage("Model loaded");

            _context = _model.CreateContext(_parameters);
            _chatHistory = new ChatHistory();

            // Add chat histories as prompt to tell AI how to act.
            _chatHistory.AddMessage(AuthorRole.System, GetSystemMessage());
            _chatHistory.AddMessage(AuthorRole.User, "Hello, Bob");
            _chatHistory.AddMessage(AuthorRole.Assistant, "Hello! How can I help?");

            _executor = new InteractiveExecutor(_context);
            _session = new(_executor, _chatHistory);
            _inferenceParams = new InferenceParams()
            {
                MaxTokens = MAX_TOKENS,
                AntiPrompts = new List<string> { "User:" }, // Stop generation once antiprompts appear.
                SamplingPipeline = samplingPipeline,
            };

            _isRunning = true;

            OutputAppMessage($"Bot Started. {modelFileInfo.Name}");
            OutputAppMessage("Ready for input");
        }
        catch (Exception ex)
        {
            _isRunning = false;
            OutputSystemMessage($"Error starting the bot. See output window.");
            OutputAppMessage($"{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }

    private string GetSystemMessage()
    {
        string systemMessage = @"Transcript of a dialog, where the User interacts with an Assistant named Bob.
Bob is helpful, kind, honest, good at writing, and never fails
to answer the User's requests immediately and with precision. Bob talks like a youngster who says dude and bro all the time.";

        return systemMessage.Replace(Environment.NewLine, " ");
    }

    public async Task ProcessInput(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput)) return;
        if(!_isRunning) return;
        
        try
        {
            await foreach (var text in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userInput), _inferenceParams, _chatCancelToken.Token))
            {
                if (_isRunning && !_chatCancelToken.IsCancellationRequested)
                {
                    if(_session != null && _chatHistory != null)
                    {
                        _chatHistory.AddMessage(AuthorRole.User, userInput);
                        OutputParialBotMessage(text);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }                   
            }

            Messenger.Send(_endBotMessageModel, 1);
        }
        catch (Exception ex)
        {
            OutputAppMessage($"{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }

    private void OutputParialBotMessage(string text)
    {
        _partialBotOutputModel.Text = text;
        Messenger.Send(_partialBotOutputModel, 1);
    }

    private void OutputSystemMessage(string text)
    {
        _systemOutputModel.Text = text;
        Messenger.Send(_systemOutputModel, 1);
    }

    private void OutputAppMessage(string text)
    {
        _appOutputModel.Text = text;
        Messenger.Send(_appOutputModel, 1);
    }

    public void KillBot()
    {
        if(!_isRunning)
        {
            Dispose();
        } else
        {
            _chatCancelToken.Cancel();
            _isRunning = false;
        }           

        OutputSystemMessage("Bot has been killed by user.");
        OutputAppMessage("Bot has been killed by user.");
    }

    public void Dispose()
    {

        if (_model != null)
        {
            _model.Dispose();
        }

        if (_context != null)
        {
            _context.Dispose();
        }

        _isRunning = false;
    }
}
