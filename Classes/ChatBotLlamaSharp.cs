using CommunityToolkit.Mvvm.ComponentModel;
using LLama;
using LLama.Common;
using LLama.Sampling;
using LLMChatTool.Models;
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
    private DefaultSamplingPipeline _samplePipeline = null;
    private float _temperature = 0.74f;
    private string _modelFullName = string.Empty;
    private bool _killBot = false;

    private EndBotMessage _endBotMessageModel = new EndBotMessage();
    private PartialBotOutputMessage _partialBotOutputModel = new PartialBotOutputMessage();
    private SystemOutputMessage _systemOutputModel = new SystemOutputMessage();

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

    public float Temperature
    {
        get
        {
            return _temperature;
        }

        set
        {
            _temperature = value;
            _samplePipeline = new DefaultSamplingPipeline()
            {
                Temperature = _temperature,
            };
        }
    }

    public void Start()
    {
        if (string.IsNullOrWhiteSpace(_modelFullName))
        {
            OutputSystemMessage("Please select a model.");
            return;
        }

        if (!File.Exists(_modelFullName))
        {
            OutputSystemMessage($"Selected model file does not exist. {_modelFullName}");
            return;
        }

        try
        {
            Dispose();

            _parameters = new ModelParams(_modelFullName)
            {
                ContextSize = CONTEXT_SIZE,
                GpuLayerCount = GPU_LAYER_COUNT
            };

            _model = LLamaWeights.LoadFromFile(_parameters);
            _context = _model.CreateContext(_parameters);
            _chatHistory = new ChatHistory();

            // Add chat histories as prompt to tell AI how to act.
            _chatHistory.AddMessage(AuthorRole.System, GetSystemMessage());
            _chatHistory.AddMessage(AuthorRole.User, "Hello, Bob");
            _chatHistory.AddMessage(AuthorRole.Assistant, "Hello! How can I help?");

            if (_samplePipeline == null)
            {
                _samplePipeline = new DefaultSamplingPipeline()
                {
                    Temperature = _temperature,
                };
            }

            _executor = new InteractiveExecutor(_context);
            _session = new(_executor, _chatHistory);
            _inferenceParams = new InferenceParams()
            {
                MaxTokens = MAX_TOKENS,
                AntiPrompts = new List<string> { "User:" }, // Stop generation once antiprompts appear.
                SamplingPipeline = _samplePipeline,
            };
        }
        catch (Exception ex)
        {
            OutputSystemMessage($"Error starting the bot. See output window.");
            OutputAppMessage($"{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }

    private string GetSystemMessage()
    {
        string systemMessage = @"Transcript of a dialog, where the User interacts with an Assistant named Bob.
Bob is helpful, kind, honest, good at writing, and never fails
to answer the User's requests immediately and with precision. Bob is a philosopher and loves to talk about the
nature of reality, human nature, quantum physics and consciousness. Bob is a spiritual guru and wants to spread
a message of love and peace.";

        return systemMessage.Replace(Environment.NewLine, " ");
    }

    public async Task ProcessInput(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput)) return;

        try
        {
            await foreach (var text in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userInput), _inferenceParams))
            {
                if (!_killBot)
                {
                    _chatHistory.AddMessage(AuthorRole.User, userInput);
                    OutputParialBotMessage(text);
                }
                else
                {
                    _killBot = false;
                    Dispose();
                    OutputSystemMessage("Bot has been killed by user.");
                    OutputAppMessage("Bot has been killed by user.");
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
        Messenger.Send(new AppOutputMessage() { Text = text }, 1);
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
    }

    public void KillBot()
    {
        _killBot = true;
    }
}
