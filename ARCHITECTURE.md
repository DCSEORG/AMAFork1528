# Azure Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                          AZURE CLOUD                                 │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │  Resource Group: rg-expensemgmt-dev (UK South)                  │ │
│  │                                                                  │ │
│  │  ┌──────────────────────────────────────────────────────────┐  │ │
│  │  │  App Service Plan (B1 - Basic)                            │  │ │
│  │  │  ┌───────────────────────────────────────────────┐        │  │ │
│  │  │  │  Web App: app-expensemgmt-dev-{timestamp}     │        │  │ │
│  │  │  │  - ASP.NET Core 8.0 Razor Pages               │        │  │ │
│  │  │  │  - REST API with Swagger                      │        │  │ │
│  │  │  │  - AI Chat UI (if enabled)                    │        │  │ │
│  │  │  └───────────────┬───────────────────────────────┘        │  │ │
│  │  └──────────────────┼──────────────────────────────────────┘  │ │
│  │                     │                                          │ │
│  │                     │ Uses                                     │ │
│  │                     ▼                                          │ │
│  │  ┌──────────────────────────────────────────────────────────┐  │ │
│  │  │  User-Assigned Managed Identity                           │  │ │
│  │  │  mid-AppModAssist-{timestamp}                            │  │ │
│  │  │  - Authenticates to Azure SQL                            │  │ │
│  │  │  - Authenticates to Azure OpenAI (if enabled)            │  │ │
│  │  │  - Authenticates to Azure AI Search (if enabled)         │  │ │
│  │  └──────────────────┬───────────────────────────────────────┘  │ │
│  │                     │                                          │ │
│  └─────────────────────┼──────────────────────────────────────────┘ │
│                        │                                            │
│                        │ Connects to                                │
│                        ▼                                            │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Azure SQL Database (Existing)                                │  │
│  │  Server: sql-expense-mgmt-xyz.database.windows.net           │  │
│  │  Database: ExpenseManagementDB                               │  │
│  │  - Tables: Users, Roles, Expenses, Categories, Status       │  │
│  │  - Managed Identity Authentication                           │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │  Gen AI Resources (Optional - when INCLUDE_CHAT_UI=true)       │ │
│  │                                                                  │ │
│  │  ┌────────────────────────────────────────────────────────┐    │ │
│  │  │  Azure OpenAI (Sweden Central)                          │    │ │
│  │  │  aoai-expensemgmt-dev-{timestamp}                      │    │ │
│  │  │  - SKU: S0                                             │    │ │
│  │  │  - Model: GPT-4o (gpt-4o deployment)                  │    │ │
│  │  │  - Managed Identity Access                            │    │ │
│  │  └────────────────┬───────────────────────────────────────┘    │ │
│  │                   │                                            │ │
│  │  ┌────────────────▼───────────────────────────────────────┐    │ │
│  │  │  Azure AI Search (UK South)                             │    │ │
│  │  │  search-expensemgmt-dev-{timestamp}                    │    │ │
│  │  │  - SKU: Basic                                          │    │ │
│  │  │  - RAG Pattern Support                                 │    │ │
│  │  │  - Managed Identity Access                             │    │ │
│  │  └────────────────────────────────────────────────────────┘    │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘

USER (Developer/End-User)
   │
   ├──► az login (Azure CLI)
   │
   ├──► deploy.sh
   │    ├─► Deploys Bicep Infrastructure
   │    ├─► Configures App Settings
   │    ├─► Runs SQL Setup Script
   │    └─► Deploys Application Code
   │
   └──► Browser
        └─► https://{app-url}/Index
            ├─► View/Manage Expenses
            ├─► Add/Submit Expenses
            ├─► Approve Expenses (Managers)
            ├─► AI Chat (if enabled)
            └─► API Docs (/swagger)
```

## Connection Flow

1. **App Service** uses **User-Assigned Managed Identity** to:
   - Connect to **Azure SQL Database** (no passwords needed)
   - Call **Azure OpenAI** for AI chat features (if enabled)
   - Access **Azure AI Search** for RAG pattern (if enabled)

2. **Database** permissions are set up via `script.sql` using the managed identity

3. **GenAI resources** are only deployed when `INCLUDE_CHAT_UI=true` in `deploy.sh`

## Security Features

- ✅ HTTPS Only
- ✅ Managed Identity Authentication (no connection strings with passwords)
- ✅ Role-Based Access Control (RBAC) for Azure resources
- ✅ Database-level permissions via managed identity
- ✅ TLS 1.2 minimum for all connections
