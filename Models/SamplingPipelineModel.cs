using LLama.Native;
using LLama.Sampling;
using System.ComponentModel;
using static LLama.Sampling.DefaultSamplingPipeline;

namespace LLMChatTool.Models;

public class SamplingPipelineModel
{
    private float _frequencyPenalty;
    private float _presencePenalty;
    private static readonly Random RandomSeedGenerator = new Random();
    //private readonly DefaultSamplingPipeline _defaultPipeline = new DefaultSamplingPipeline();
    //private DefaultSamplingPipeline _workingSamplingPipeline;

    [Description($"Bias values to add to certain logits\nType: IReadOnlyDictionary<LLamaToken, float>")]
    public IReadOnlyDictionary<LLamaToken, float> LogitBias { get; set; } = new Dictionary<LLamaToken, float>();

    //"as described in https://arxiv.org/abs/1909.05858"
    [Description("Repetition penalty\nType: float")]
    public float RepeatPenalty { get; set; } = 1f;

    //as described by OpenAI: https://platform.openai.com/docs/api-reference/chat/create
    [Description(@"Frequency penalty 
Number between -2.0 and 2.0. Positive values penalize new tokens based on their
existing frequency in the text so far, decreasing the model's likelihood to repeat
the same line verbatim. 
Type: float")]

    public float FrequencyPenalty
    {
        get
        {
            return _frequencyPenalty;
        }
        set
        {
            if (value < -2f)
            {
                throw new ArgumentOutOfRangeException("value", "FrequencyPenalty must be greater than -2");
            }

            if (value > 2f)
            {
                throw new ArgumentOutOfRangeException("value", "FrequencyPenalty must be less than 2");
            }

            _frequencyPenalty = value;
        }
    }

    // as described by OpenAI: https://platform.openai.com/docs/api-reference/chat/create
    [Description(@"Presence penalty
Number between -2.0 and 2.0. Positive values penalize new tokens based on whether
they appear in the text so far, increasing the model's likelihood to talk about
new topics.
Type: float")]
    public float PresencePenalty
    {
        get
        {
            return _presencePenalty;
        }
        set
        {
            if (value < -2f)
            {
                throw new ArgumentOutOfRangeException("value", "PresencePenalty must be greater than -2");
            }

            if (value > 2f)
            {
                throw new ArgumentOutOfRangeException("value", "PresencePenalty must be less than 2");
            }

            _presencePenalty = value;
        }
    }

    [Description("How many tokens should be considered for penalties\nType: int")]
    public int PenaltyCount { get; set; } = 64;

    [Description("Whether the newline token should be protected from being modified by penalty\nType: bool")]
    public bool PenalizeNewline { get; set; }

    [Description("Whether the EOS token should be suppressed. Setting this to 'true' prevents EOS from being sampled\nType: bool")]
    public bool PreventEOS { get; set; }

    [Description("Temperature to apply. A decimal between 0 and 1. (higher temperature is more \"creative\")\nType: float")]
    public float Temperature { get; set; } = 0.75f;

    [Description("Number of tokens to keep in TopK sampling\nType: int")]
    public int TopK { get; set; } = 40;

    [Description("P value for locally typical sampling\nType: float")]
    public float TypicalP { get; set; } = 1f;


    [Description("P value for TopP sampling\nType: float")]
    public float TopP { get; set; } = 0.9f;

    [Description("P value for MinP sampling\nType: float")]
    public float MinP { get; set; } = 0.1f;

    [Description("Grammar to apply to constrain possible tokens\nType: Grammar?")]
    public Grammar? Grammar { get; set; }

    [Description("The minimum number of tokens to keep for samplers which remove tokens\nType: int")]
    public int MinKeep { get; set; } = 1;

    [Description("Seed to use for random sampling\nType: uint")]
    public uint Seed { get; set; } = GetRandomSeed();


    [Description("Selected grammar optimization mode\nType: GrammarOptimizationMode")]
    public GrammarOptimizationMode GrammarOptimization { get; set; } = GrammarOptimizationMode.Extended;

    public SamplingPipelineModel()
    {
        FillProperties(new DefaultSamplingPipeline());
    }

    public SamplingPipelineModel(DefaultSamplingPipeline samplingPipeline)
    {
        FillProperties(samplingPipeline);
    }

    public DefaultSamplingPipeline ToDefaultSamplingPipeline()
    {
        return new DefaultSamplingPipeline()
        {
            FrequencyPenalty = FrequencyPenalty,
            Grammar = Grammar,
            GrammarOptimization = GrammarOptimization,
            LogitBias = LogitBias,
            MinKeep = MinKeep,
            MinP = MinP,
            PenalizeNewline = PenalizeNewline,
            PenaltyCount = PenaltyCount,
            PresencePenalty = PresencePenalty,
            PreventEOS = PreventEOS,
            RepeatPenalty = RepeatPenalty,
            Seed = Seed,
            Temperature = Temperature,
            TopK = TopK,
            TopP = TopP,
            TypicalP = TypicalP
        };
    }

    private void FillProperties(DefaultSamplingPipeline samplingPipline)
    {
        // --- floats
        FrequencyPenalty = samplingPipline.FrequencyPenalty;
        MinP = samplingPipline.MinP;
        PresencePenalty = samplingPipline.PresencePenalty;
        RepeatPenalty = samplingPipline.RepeatPenalty;
        Temperature = samplingPipline.Temperature;
        TopP = samplingPipline.TopP;
        TypicalP = samplingPipline.TypicalP;

        // --- ints
        MinKeep = samplingPipline.MinKeep;
        PenaltyCount = samplingPipline.PenaltyCount;
        Seed = samplingPipline.Seed;
        TopK = samplingPipline.TopK;

        // --- other types
        Grammar = samplingPipline.Grammar;
        GrammarOptimization = samplingPipline.GrammarOptimization;
        LogitBias = samplingPipline.LogitBias;       
       
        //--- bools
        PenalizeNewline = samplingPipline.PenalizeNewline;
        PreventEOS = samplingPipline.PreventEOS;      
    }

    private static uint GetRandomSeed()
    {
        lock (RandomSeedGenerator)
        {
            return (uint)(RandomSeedGenerator.Next(0, int.MaxValue) + RandomSeedGenerator.Next(0, int.MaxValue));
        }
    }
}
