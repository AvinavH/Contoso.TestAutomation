using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Contoso.Automation.Core.Configuration;
using Serilog;

namespace Contoso.Automation.AI.Services;

/// <summary>
/// Provides Claude AI capabilities within the test framework.
/// Used by agents for intelligent test data generation and self-healing locators.
///
/// Graceful degradation: when AI is disabled (no API key configured), methods
/// return null/empty rather than throwing, so AI features degrade without
/// blocking test execution. This is critical for environments without API access.
/// </summary>
public sealed class ClaudeAIService
{
    private readonly AISettings _settings;
    private readonly AnthropicClient? _client;
    private readonly ILogger _log = Log.ForContext<ClaudeAIService>();

    public bool IsEnabled => _settings.Enabled && !string.IsNullOrEmpty(_settings.AnthropicApiKey);

    public ClaudeAIService(TestConfiguration config)
    {
        _settings = config.AI;

        if (IsEnabled)
        {
            _client = new AnthropicClient(_settings.AnthropicApiKey);
            _log.Information("Claude AI service initialised (model: {Model})", _settings.Model);
        }
        else
        {
            _log.Warning("Claude AI service disabled - AI__AnthropicApiKey not configured. AI features will degrade gracefully.");
        }
    }

    /// <summary>
    /// Sends a single prompt to Claude and returns the text response.
    /// Returns null if AI is disabled or the call fails.
    /// </summary>
    public async Task<string?> CompleteAsync(string prompt, string? systemPrompt = null)
    {
        if (!IsEnabled || _client is null) return null;

        try
        {
            var messages = new List<Message>
            {
                new Message { Role = RoleType.User, Content = new List<ContentBase> { new TextContent { Text = prompt } } }
            };

            var request = new MessageParameters
            {
                Model     = _settings.Model,
                MaxTokens = _settings.MaxTokens,
                Messages  = messages,
                System    = systemPrompt is not null
                    ? new List<SystemMessage> { new SystemMessage(systemPrompt) }
                    : null
            };

            var response = await _client.Messages.GetClaudeMessageAsync(request);
            var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text;

            _log.Debug("Claude response received ({Tokens} tokens)", response.Usage?.OutputTokens);
            return text;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Claude API call failed - continuing without AI assistance");
            return null;
        }
    }

    /// <summary>
    /// Sends a structured prompt expecting a JSON response.
    /// Strips markdown code fences if Claude wraps the JSON.
    /// Returns null if AI is disabled or the response cannot be parsed.
    /// </summary>
    public async Task<string?> CompleteAsJsonAsync(string prompt, string? systemPrompt = null)
    {
        var jsonSystemPrompt = (systemPrompt ?? string.Empty) +
            "\n\nIMPORTANT: Respond ONLY with valid JSON. No preamble, no explanation, no markdown code blocks.";

        var raw = await CompleteAsync(prompt, jsonSystemPrompt.Trim());
        if (raw is null) return null;

        // Strip markdown code fences if present
        return raw
            .Replace("```json", string.Empty)
            .Replace("```", string.Empty)
            .Trim();
    }
}
