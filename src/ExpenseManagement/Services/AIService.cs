using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace ExpenseManagement.Services;

public class AIService
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _dbService;
    private readonly ILogger<AIService> _logger;
    private readonly string? _systemContext;

    public AIService(IConfiguration configuration, DatabaseService dbService, ILogger<AIService> logger)
    {
        _configuration = configuration;
        _dbService = dbService;
        _logger = logger;

        // Load RAG context
        var contextPath = Path.Combine(AppContext.BaseDirectory, "RAG", "expense-system-context.md");
        if (File.Exists(contextPath))
        {
            _systemContext = File.ReadAllText(contextPath);
        }
    }

    public async Task<string> ProcessChatMessageAsync(string userMessage, int userId = 1)
    {
        try
        {
            var endpoint = _configuration["OpenAI:Endpoint"];
            var deploymentName = _configuration["OpenAI:DeploymentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
            {
                return "⚠️ Chat UI is not configured. Please deploy with INCLUDE_CHAT_UI=true in deploy.sh";
            }

            // Check if message is asking for data
            var lowerMessage = userMessage.ToLower();
            string? dataContext = null;

            // Get relevant data based on query
            if (lowerMessage.Contains("expense") || lowerMessage.Contains("my") || lowerMessage.Contains("show"))
            {
                var expenses = await _dbService.GetExpensesAsync(userId: userId);
                dataContext = $"\n\nCurrent user's expenses:\n{JsonSerializer.Serialize(expenses, new JsonSerializerOptions { WriteIndented = true })}";
            }

            if (lowerMessage.Contains("categor"))
            {
                var categories = await _dbService.GetCategoriesAsync();
                dataContext = (dataContext ?? "") + $"\n\nAvailable categories:\n{JsonSerializer.Serialize(categories)}";
            }

            if (lowerMessage.Contains("pending") || lowerMessage.Contains("approv"))
            {
                var pending = await _dbService.GetExpensesAsync(statusId: 2); // Submitted
                dataContext = (dataContext ?? "") + $"\n\nPending expenses:\n{JsonSerializer.Serialize(pending, new JsonSerializerOptions { WriteIndented = true })}";
            }

            var client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
            var chatClient = client.GetChatClient(deploymentName);

            var systemMessage = $@"You are an AI assistant for an expense management system. 
You help users understand and manage their expenses through natural language.

{_systemContext}

The current user is userId={userId}.

When users ask questions:
1. Use the provided data context to answer
2. Provide clear, friendly responses
3. Format currency as £X.XX
4. Explain what actions they can take

Note: You can view data but cannot directly modify it through this interface. 
For creating or updating expenses, guide users to use the web interface or API.
{dataContext}";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemMessage),
                new UserChatMessage(userMessage)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 500,
                Temperature = 0.7f
            };

            var response = await chatClient.CompleteChatAsync(messages, options);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return $"❌ Error: {ex.Message}";
        }
    }
}
