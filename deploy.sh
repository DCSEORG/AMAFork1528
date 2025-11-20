#!/bin/bash
set -e

# Configuration
INCLUDE_CHAT_UI=false  # Set to true to deploy GenAI resources
RESOURCE_GROUP="rg-expensemgmt-dev"
LOCATION="uksouth"
BASE_NAME="expensemgmt"
ENVIRONMENT="dev"

echo "=========================================="
echo "Expense Management App Deployment"
echo "=========================================="
echo "Include Chat UI: $INCLUDE_CHAT_UI"
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo ""

# Deploy infrastructure
echo "Deploying infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment sub create \
  --location $LOCATION \
  --template-file infrastructure/main.bicep \
  --parameters resourceGroupName=$RESOURCE_GROUP \
               location=$LOCATION \
               baseName=$BASE_NAME \
               environment=$ENVIRONMENT \
               deployGenAI=$INCLUDE_CHAT_UI \
  --query "properties.outputs" \
  --output json)

echo "✓ Infrastructure deployed successfully"

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')

echo ""
echo "Deployment outputs:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  App URL: $APP_SERVICE_URL"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  MI Client ID: $MANAGED_IDENTITY_CLIENT_ID"

# Configure OpenAI settings if GenAI is deployed
if [ "$INCLUDE_CHAT_UI" = true ]; then
  OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
  OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
  SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')
  
  echo ""
  echo "Configuring Gen AI settings..."
  az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --settings \
      "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
      "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
      "Search__Endpoint=$SEARCH_ENDPOINT" \
    --output none
  
  echo "✓ Gen AI settings configured"
  echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
  echo "  Model: $OPENAI_MODEL_NAME"
fi

# Install required Python packages if not already installed
echo ""
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Update script.sql with actual managed identity name
echo "Updating SQL script with managed identity name..."
sed -i "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql

# Run the SQL setup script
echo ""
echo "Setting up database permissions..."
python3 run-sql.py

echo ""
echo "=========================================="
echo "Building and deploying application..."
echo "=========================================="

# Build the application
cd src/ExpenseManagement
dotnet publish -c Release -o ../../publish

# Create deployment zip (files at root, not in subdirectory)
cd ../../publish
zip -r ../app.zip . > /dev/null
cd ..

echo "✓ Application built and packaged"

# Deploy the application
echo ""
echo "Deploying application to Azure..."
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip \
  --type zip

echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Application URL: $APP_SERVICE_URL/Index"
echo ""
echo "Note: Navigate to /Index to view the application"
echo ""
if [ "$INCLUDE_CHAT_UI" = true ]; then
  echo "Chat UI is enabled at: $APP_SERVICE_URL/Chat"
else
  echo "To enable Chat UI, set INCLUDE_CHAT_UI=true and redeploy"
fi
echo ""
