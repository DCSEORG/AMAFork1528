![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

# Expense Management System - Modern Azure Application

A modern, cloud-native expense management system built with ASP.NET Core 8.0, deployed to Azure App Service with optional AI-powered chat interface.

## ✨ Features

- 📝 **Employee Expense Management**: Submit, track, and manage expenses
- ✅ **Manager Approval Workflow**: Review and approve/reject submitted expenses
- 🤖 **AI Chat Assistant** (Optional): Natural language interface powered by Azure OpenAI GPT-4o
- 🔒 **Secure**: Managed Identity authentication, no passwords in code
- 🚀 **Modern UI**: Clean, responsive Bootstrap 5 interface
- 📊 **REST API**: Full API with Swagger documentation
- 💾 **Azure SQL Database**: Existing database integration with RAG context

## 🏗️ Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed architecture diagram and component descriptions.

**Key Components:**
- **Azure App Service** (B1 tier) - Hosts the web application
- **User-Assigned Managed Identity** - Secure authentication
- **Azure SQL Database** (Existing) - Data storage
- **Azure OpenAI** (Optional, S0 tier) - GPT-4o model for AI chat
- **Azure AI Search** (Optional, Basic tier) - RAG pattern support

## 📋 Prerequisites

- Azure subscription
- Azure CLI installed and configured
- .NET 8.0 SDK (for local development)
- Python 3.x with pip (for database setup)
- Existing Azure SQL Database (Server: sql-expense-mgmt-xyz.database.windows.net)

## 🚀 Quick Start Deployment

### Step 1: Fork and Clone

```bash
# Fork this repo first, then clone your fork
git clone <your-fork-url>
cd <your-repo-name>
```

### Step 2: Login to Azure

```bash
az login
az account set --subscription <your-subscription-id>
```

### Step 3: Deploy (Without AI Chat)

```bash
chmod +x deploy.sh
./deploy.sh
```

This will:
1. ✅ Deploy infrastructure (App Service, Managed Identity)
2. ✅ Configure database permissions
3. ✅ Build and deploy the application
4. ✅ Display your app URL

**Access your app at:** `https://{app-service-name}.azurewebsites.net/Index`

### Step 4: Deploy With AI Chat (Optional)

To enable the AI-powered chat interface:

```bash
# Edit deploy.sh and change INCLUDE_CHAT_UI to true
nano deploy.sh  # or your preferred editor
# Change: INCLUDE_CHAT_UI=false
# To: INCLUDE_CHAT_UI=true

# Then run deployment
./deploy.sh
```

This additionally deploys:
- Azure OpenAI (GPT-4o model in Sweden)
- Azure AI Search (for RAG pattern)
- Configures managed identity access

## 📱 Application Features

### For Employees:
1. **View Expenses** (`/Index`) - See all your expenses with filtering
2. **Add Expense** (`/AddExpense`) - Create new expense submissions
3. **Submit Expenses** - Move expenses from Draft to Submitted status
4. **AI Chat** (`/Chat`, if enabled) - Ask questions like "Show me my expenses"

### For Managers:
1. **Approve Expenses** (`/ApproveExpenses`) - Review and approve/reject submitted expenses
2. **AI Chat** (`/Chat`, if enabled) - Ask "What pending expenses need approval?"

### API Documentation:
- **Swagger UI**: `https://{your-app}.azurewebsites.net/swagger`
- Full REST API with endpoints for:
  - GET `/api/expenses` - List expenses
  - POST `/api/expenses` - Create expense
  - PUT `/api/expenses/{id}/status` - Update status
  - GET `/api/categories` - List categories
  - GET `/api/users` - List users

## 🗂️ Database Schema

The system connects to an existing Azure SQL Database with the following schema:

- **Roles**: Employee, Manager
- **Users**: User accounts with role assignments
- **ExpenseCategories**: Travel, Meals, Supplies, Accommodation, Other
- **ExpenseStatus**: Draft, Submitted, Approved, Rejected
- **Expenses**: Main expense records with amounts stored in pence

See `Database-Schema/database_schema.sql` for full schema details.

## 🔧 Configuration

### App Settings (Configured automatically by deploy.sh):
```json
{
  "ConnectionStrings": {
    "ExpenseDb": "Server=...;Database=ExpenseManagementDB;..."
  },
  "ManagedIdentityClientId": "{auto-configured}",
  "EnableChatUI": true,
  "OpenAI": {
    "Endpoint": "{auto-configured-if-enabled}",
    "DeploymentName": "gpt-4o"
  }
}
```

### Customization:
- **Resource Group**: Edit `RESOURCE_GROUP` in `deploy.sh`
- **Location**: Edit `LOCATION` in `deploy.sh` (default: uksouth)
- **Database Connection**: Edit `appsettings.json` or `ConnectionStrings:ExpenseDb`

## 🔒 Security Features

- ✅ **Managed Identity Authentication**: No passwords stored in code or config
- ✅ **HTTPS Only**: All traffic encrypted
- ✅ **TLS 1.2+**: Minimum TLS version enforced
- ✅ **RBAC**: Role-based access control for Azure resources
- ✅ **Error Handling**: Graceful fallback to dummy data if database unavailable
- ✅ **SQL Injection Protection**: Parameterized queries throughout

## 🐛 Troubleshooting

### Database Connection Issues:
- Check that `script.sql` ran successfully with the correct managed identity name
- Verify the managed identity has `db_datareader` and `db_datawriter` roles
- Check firewall rules on Azure SQL Server

### AI Chat Not Working:
- Ensure `INCLUDE_CHAT_UI=true` in `deploy.sh`
- Verify Azure OpenAI deployment completed successfully
- Check that managed identity has "Cognitive Services OpenAI User" role

### App Not Loading:
- Navigate to `/Index` not just root URL
- Check App Service logs in Azure Portal
- Verify `app.zip` deployed correctly

## 📁 Project Structure

```
├── infrastructure/           # Bicep IaC files
│   ├── main.bicep           # Main deployment orchestration
│   ├── app-service.bicep    # App Service & Managed Identity
│   └── genai.bicep          # Azure OpenAI & AI Search
├── src/ExpenseManagement/   # ASP.NET Core application
│   ├── Controllers/         # API controllers
│   ├── Models/              # Data models
│   ├── Pages/               # Razor Pages
│   ├── Services/            # Business logic (Database, AI)
│   └── RAG/                 # Context for AI chat
├── Database-Schema/         # SQL schema and samples
├── deploy.sh               # Main deployment script
├── run-sql.py              # Database setup script
└── app.zip                 # Deployment package

```

## 🤝 Contributing

When testing or contributing:
1. Fork the repo (don't work directly on App-Mod-Assist base)
2. Name your fork something unique (e.g., `AMA-YourName-Test`)
3. Make changes in your fork
4. Test with the coding agent
5. Submit PR if contributing back

## 📄 License

See [LICENSE](./LICENSE) for details.

## 🆘 Support

For issues or questions:
1. Check [ARCHITECTURE.md](./ARCHITECTURE.md) for technical details
2. Review deployment logs: `az webapp log tail --name {app-name} --resource-group {rg-name}`
3. Check Azure Portal for resource status

---

**Note**: This is a proof-of-concept application for demonstrating Azure app modernization patterns. For production use, review the PRODUCTION_CONSIDERATIONS section in deployment documentation.
