using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using System.Text.Json;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    private readonly AIService _aiService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatModel> _logger;

    public ChatModel(AIService aiService, IConfiguration configuration, ILogger<ChatModel> logger)
    {
        _aiService = aiService;
        _configuration = configuration;
        _logger = logger;
    }

    public bool ChatEnabled { get; set; }

    public void OnGet()
    {
        var enableChatUI = _configuration.GetValue<bool>("EnableChatUI");
        var hasEndpoint = !string.IsNullOrEmpty(_configuration["OpenAI:Endpoint"]);
        ChatEnabled = enableChatUI && hasEndpoint;
    }

    public async Task<IActionResult> OnPostSendMessageAsync([FromBody] ChatRequest request)
    {
        try
        {
            var response = await _aiService.ProcessChatMessageAsync(request.Message, userId: 1);
            return new JsonResult(new { response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return new JsonResult(new { response = $"❌ Error: {ex.Message}" });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
