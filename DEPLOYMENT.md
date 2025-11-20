# Deployment Guide - Expense Management System

This guide provides step-by-step instructions for deploying the Expense Management System to Azure.

## Prerequisites Checklist

Before deploying, ensure you have:

- ☐ Azure subscription with appropriate permissions
- ☐ Azure CLI installed (`az --version` to verify)
- ☐ .NET 8.0 SDK installed (for local testing)
- ☐ Python 3.x with pip installed
- ☐ Access to existing Azure SQL Database
  - Server: `sql-expense-mgmt-xyz.database.windows.net`
  - Database: `ExpenseManagementDB`
  - Firewall rules configured to allow your IP

## Deployment Steps

### 1. Prepare Your Environment

```bash
# Clone the repository
git clone <your-repo-url>
cd <repo-directory>

# Login to Azure
az login

# Set your subscription (if you have multiple)
az account list --output table
az account set --subscription "<subscription-id-or-name>"

# Verify you're in the correct subscription
az account show --output table
```

### 2. Configure Deployment Settings (Optional)

Edit `deploy.sh` to customize settings:

```bash
nano deploy.sh  # or use your preferred editor
```

**Configuration Options:**
```bash
INCLUDE_CHAT_UI=false    # Set to 'true' to deploy AI chat features
RESOURCE_GROUP="rg-expensemgmt-dev"  # Change if desired
LOCATION="uksouth"       # Change region if desired
BASE_NAME="expensemgmt"  # Change base name if desired
ENVIRONMENT="dev"        # Change environment suffix
```

### 3. Deploy Basic Application (Without AI)

```bash
# Make deployment script executable
chmod +x deploy.sh

# Run deployment
./deploy.sh
```

**What happens during deployment:**
1. ✅ Creates resource group (if doesn't exist)
2. ✅ Deploys App Service Plan (B1 tier)
3. ✅ Creates App Service with Linux/.NET 8.0
4. ✅ Creates user-assigned managed identity
5. ✅ Installs Python dependencies (pyodbc, azure-identity)
6. ✅ Updates `script.sql` with managed identity name
7. ✅ Runs SQL script to grant database permissions
8. ✅ Builds and publishes .NET application
9. ✅ Creates app.zip deployment package
10. ✅ Deploys application to App Service

**Expected Output:**
```
==========================================
Deployment Complete!
==========================================

Application URL: https://app-expensemgmt-dev-{timestamp}.azurewebsites.net/Index

Note: Navigate to /Index to view the application
```

### 4. Deploy With AI Chat Features (Optional)

```bash
# Edit deploy.sh
nano deploy.sh

# Change INCLUDE_CHAT_UI from false to true
INCLUDE_CHAT_UI=true

# Save and run deployment
./deploy.sh
```

**Additional resources deployed:**
- ✅ Azure OpenAI account (S0 tier, Sweden Central)
- ✅ GPT-4o model deployment
- ✅ Azure AI Search service (Basic tier, UK South)
- ✅ Managed identity role assignments
- ✅ App Service configuration for OpenAI endpoint

**Note:** AI deployment adds ~5-10 minutes to deployment time.

## Post-Deployment Verification

### 1. Test Web Application

```bash
# Get your app URL
az webapp show --name <app-service-name> --resource-group rg-expensemgmt-dev --query "defaultHostName" -o tsv

# Open in browser
# https://<app-service-name>.azurewebsites.net/Index
```

**Pages to Test:**
- ✅ `/Index` - Expenses list
- ✅ `/AddExpense` - Add new expense
- ✅ `/ApproveExpenses` - Approve expenses (manager view)
- ✅ `/Chat` - AI chat (if enabled)
- ✅ `/swagger` - API documentation

### 2. Test API

```bash
# Test health of API
APP_URL="https://<your-app>.azurewebsites.net"

# Get categories
curl $APP_URL/api/categories

# Get expenses
curl $APP_URL/api/expenses
```

### 3. Check Logs (If Issues)

```bash
# Stream logs in real-time
az webapp log tail --name <app-service-name> --resource-group rg-expensemgmt-dev

# Download logs
az webapp log download --name <app-service-name> --resource-group rg-expensemgmt-dev --log-file logs.zip
```

## Common Deployment Issues

### Issue 1: Database Connection Fails

**Symptoms:**
- Error messages about database connectivity
- Dummy data displayed instead of real data

**Solutions:**
1. Verify firewall rules:
   ```bash
   # Add your IP to SQL firewall
   MY_IP=$(curl -s ifconfig.me)
   az sql server firewall-rule create \
     --resource-group <sql-rg> \
     --server sql-expense-mgmt-xyz \
     --name "MyWorkstation" \
     --start-ip-address $MY_IP \
     --end-ip-address $MY_IP
   ```

2. Check managed identity permissions:
   ```bash
   # Rerun the SQL script
   python3 run-sql.py
   ```

3. Verify connection string in App Service configuration

### Issue 2: Python Dependencies Not Installing

**Symptoms:**
- Error during `pip3 install` step
- `run-sql.py` fails

**Solutions:**
```bash
# Install manually
pip3 install --user pyodbc azure-identity

# On Ubuntu/Debian, you may need ODBC drivers:
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list | sudo tee /etc/apt/sources.list.d/mssql-release.list
sudo apt-get update
sudo ACCEPT_EULA=Y apt-get install -y msodbcsql18
```

### Issue 3: App Service Not Starting

**Symptoms:**
- 503 Service Unavailable
- Application Error page

**Solutions:**
1. Check deployment:
   ```bash
   az webapp deployment list-publishing-credentials \
     --name <app-name> \
     --resource-group rg-expensemgmt-dev
   ```

2. Verify app settings:
   ```bash
   az webapp config appsettings list \
     --name <app-name> \
     --resource-group rg-expensemgmt-dev
   ```

3. Restart the app:
   ```bash
   az webapp restart \
     --name <app-name> \
     --resource-group rg-expensemgmt-dev
   ```

### Issue 4: AI Chat Not Working

**Symptoms:**
- Chat page shows configuration warning
- AI responses return errors

**Solutions:**
1. Verify `INCLUDE_CHAT_UI=true` was used in deployment

2. Check OpenAI deployment:
   ```bash
   az cognitiveservices account list \
     --resource-group rg-expensemgmt-dev
   ```

3. Verify app settings for OpenAI:
   ```bash
   az webapp config appsettings list \
     --name <app-name> \
     --resource-group rg-expensemgmt-dev \
     --query "[?name=='OpenAI__Endpoint' || name=='OpenAI__DeploymentName']"
   ```

4. Check managed identity has "Cognitive Services OpenAI User" role

## Manual Configuration (Advanced)

If automatic deployment fails, you can configure manually:

### Set App Service Configuration

```bash
APP_NAME="<your-app-name>"
RG="rg-expensemgmt-dev"

# Set managed identity client ID
MI_CLIENT_ID=$(az identity show --name <mi-name> --resource-group $RG --query clientId -o tsv)
az webapp config appsettings set --name $APP_NAME --resource-group $RG \
  --settings "ManagedIdentityClientId=$MI_CLIENT_ID"

# If using AI Chat
OPENAI_ENDPOINT=$(az cognitiveservices account show --name <openai-name> --resource-group $RG --query properties.endpoint -o tsv)
az webapp config appsettings set --name $APP_NAME --resource-group $RG \
  --settings "OpenAI__Endpoint=$OPENAI_ENDPOINT" "OpenAI__DeploymentName=gpt-4o"
```

### Grant Database Permissions Manually

```sql
-- Connect to ExpenseManagementDB as admin
-- Run in SQL Server Management Studio or Azure Data Studio

CREATE USER [<managed-identity-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<managed-identity-name>];
ALTER ROLE db_datawriter ADD MEMBER [<managed-identity-name>];
GO
```

## Cost Estimation

**Without AI Chat (Basic):**
- App Service (B1): ~£40/month
- Managed Identity: Free
- SQL Database: Existing/Shared
- **Total: ~£40/month**

**With AI Chat:**
- App Service (B1): ~£40/month
- Azure OpenAI (S0): ~£0.50/1K tokens
- Azure AI Search (Basic): ~£60/month
- Managed Identity: Free
- **Total: ~£100-150/month** (depending on AI usage)

**Note:** Prices are estimates. Check Azure pricing calculator for accurate costs.

## Cleanup

To remove all deployed resources:

```bash
# Delete entire resource group (CAUTION: Irreversible!)
az group delete --name rg-expensemgmt-dev --yes --no-wait

# Or delete individual resources
az webapp delete --name <app-name> --resource-group rg-expensemgmt-dev
az appservice plan delete --name <plan-name> --resource-group rg-expensemgmt-dev
az cognitiveservices account delete --name <openai-name> --resource-group rg-expensemgmt-dev
az search service delete --name <search-name> --resource-group rg-expensemgmt-dev
az identity delete --name <mi-name> --resource-group rg-expensemgmt-dev
```

## Next Steps

After successful deployment:

1. ✅ Test all application features
2. ✅ Review security settings
3. ✅ Set up monitoring and alerts
4. ✅ Configure custom domain (optional)
5. ✅ Set up CI/CD pipeline (optional)
6. ✅ Review cost management

## Support

For issues:
1. Check logs: `az webapp log tail`
2. Review [README.md](./README.md)
3. Check [ARCHITECTURE.md](./ARCHITECTURE.md)
4. Review Azure Portal for resource status

---

**Security Note:** This is a development/POC deployment. For production:
- Use higher-tier App Service plans
- Enable Application Insights
- Set up proper monitoring and alerts
- Implement proper authentication (Azure AD)
- Review and harden security settings
- Implement backup and disaster recovery
