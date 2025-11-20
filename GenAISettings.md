# GenAI Settings Reference

This file documents the configuration settings for the AI chat functionality.

## Configuration Structure

The AI chat feature uses the following configuration settings in `appsettings.json`:

```json
{
  "EnableChatUI": true,
  "OpenAI": {
    "Endpoint": "https://aoai-expensemgmt-dev-{timestamp}.openai.azure.com/",
    "DeploymentName": "gpt-4o"
  },
  "Search": {
    "Endpoint": "https://search-expensemgmt-dev-{timestamp}.search.windows.net"
  },
  "ManagedIdentityClientId": "{managed-identity-client-id}"
}
```

## Settings Explanation

### EnableChatUI
- **Type**: Boolean
- **Default**: `true`
- **Purpose**: Enables or disables the chat UI feature in the application
- **Note**: Even if set to true, the chat won't work without proper OpenAI configuration

### OpenAI.Endpoint
- **Type**: String (URL)
- **Format**: `https://{resource-name}.openai.azure.com/`
- **Purpose**: The endpoint URL for your Azure OpenAI resource
- **Configured by**: `deploy.sh` when `INCLUDE_CHAT_UI=true`
- **Region**: Deployed to Sweden Central (swedencentral)

### OpenAI.DeploymentName
- **Type**: String
- **Value**: `gpt-4o`
- **Purpose**: The name of the model deployment within Azure OpenAI
- **Model**: GPT-4o (latest version)
- **Configured by**: `deploy.sh` automatically

### Search.Endpoint
- **Type**: String (URL)
- **Format**: `https://{resource-name}.search.windows.net`
- **Purpose**: Azure AI Search endpoint for RAG pattern implementation
- **Configured by**: `deploy.sh` when `INCLUDE_CHAT_UI=true`
- **Region**: Deployed to UK South (uksouth)

### ManagedIdentityClientId
- **Type**: String (GUID)
- **Purpose**: Client ID of the user-assigned managed identity for authentication
- **Used for**: 
  - Azure SQL Database authentication
  - Azure OpenAI authentication
  - Azure AI Search authentication
- **Configured by**: `deploy.sh` automatically

## Authentication

The application uses **Managed Identity authentication** for all Azure services:

- ✅ No API keys stored in configuration
- ✅ Automatic token acquisition using `DefaultAzureCredential`
- ✅ Role-based access control (RBAC)

### Required Roles

The managed identity needs the following role assignments:

1. **Azure SQL Database**:
   - Database: `db_datareader`, `db_datawriter` (set via SQL script)

2. **Azure OpenAI**:
   - Role: `Cognitive Services OpenAI User`
   - Scope: Azure OpenAI resource

3. **Azure AI Search**:
   - Role: `Search Service Contributor`
   - Scope: Search service resource

## RAG Pattern Implementation

The chat AI uses a Retrieval-Augmented Generation pattern:

### Context Sources

1. **Static Context File**: `RAG/expense-system-context.md`
   - Database schema information
   - Common operations and examples
   - Natural language to API translation guides

2. **Dynamic Data Context**:
   - User's expenses (fetched from database)
   - Categories (fetched from database)
   - Pending approvals (fetched from database)

### Context Injection

When a user sends a message, the system:
1. Analyzes the message for keywords
2. Fetches relevant data from the database
3. Combines static context + dynamic data
4. Sends to Azure OpenAI with system prompt
5. Returns AI-generated response

## Usage Examples

### Deployment with AI Chat

```bash
# Edit deploy.sh
INCLUDE_CHAT_UI=true

# Deploy
./deploy.sh
```

This will:
1. Create Azure OpenAI resource in Sweden Central
2. Deploy GPT-4o model
3. Create Azure AI Search service
4. Grant managed identity access
5. Configure app settings with endpoints

### Deployment without AI Chat (Default)

```bash
# deploy.sh has INCLUDE_CHAT_UI=false by default
./deploy.sh
```

This will:
1. Skip GenAI resource deployment
2. Leave OpenAI settings empty in configuration
3. Chat page will show configuration message
4. All other features work normally

## Manual Configuration

If you need to update settings manually:

```bash
APP_NAME="your-app-name"
RG="rg-expensemgmt-dev"

# Update OpenAI endpoint
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RG \
  --settings "OpenAI__Endpoint=https://your-openai.openai.azure.com/"

# Update deployment name
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RG \
  --settings "OpenAI__DeploymentName=gpt-4o"

# Enable/disable chat UI
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RG \
  --settings "EnableChatUI=true"
```

## Troubleshooting

### Chat shows "not configured" message

**Check:**
1. Is `INCLUDE_CHAT_UI=true` in your deployment?
2. Do the app settings exist?
   ```bash
   az webapp config appsettings list --name $APP_NAME --resource-group $RG \
     --query "[?name=='OpenAI__Endpoint']"
   ```

### Chat returns authentication errors

**Check:**
1. Managed identity role assignments:
   ```bash
   az role assignment list --assignee <managed-identity-id> --all
   ```
2. Verify "Cognitive Services OpenAI User" role is assigned

### Chat responses are slow

**This is normal:**
- GPT-4o responses typically take 2-5 seconds
- Larger context (more expenses) = slower responses
- Consider caching for repeated queries

## Cost Optimization

**AI Chat Usage Costs:**
- Input tokens: ~$0.0025 per 1K tokens
- Output tokens: ~$0.01 per 1K tokens

**Tips:**
- Limit context size by filtering data
- Use concise prompts
- Consider caching common queries
- Monitor usage in Azure Portal

## Security Considerations

1. **No API Keys**: All authentication via managed identity
2. **Data Privacy**: User data only sent to Azure OpenAI (within your tenant)
3. **Token Limits**: 500 max output tokens set to control costs
4. **Error Handling**: Graceful degradation if OpenAI unavailable

## Advanced Configuration

For production deployments, consider:

1. **Content Filtering**: Configure Azure OpenAI content filters
2. **Rate Limiting**: Implement request throttling
3. **Caching**: Cache AI responses for common queries
4. **Monitoring**: Track token usage and costs
5. **Fallback**: Implement fallback responses if AI unavailable

## References

- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Managed Identity Best Practices](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview)
- [Azure AI Search Documentation](https://learn.microsoft.com/en-us/azure/search/)
- [RAG Pattern Guide](https://learn.microsoft.com/en-us/azure/search/retrieval-augmented-generation-overview)
