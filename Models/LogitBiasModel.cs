using LLama.Native;

namespace LLMChatTool.Models;

public class LogitBiasModel
{
    public LLamaToken LLamaToken { get; set; }
    public float Value { get; set; }
}
