# EventGrid

```azurecli-interactive

az group create --name EventGridTest --location westeurope

az storage account create --name <storage_name> --location westeurope --resource-group EventGridTest --sku Standard_LRS

az functionapp create --name <app_name> --storage-account <storage_name> --resource-group EventGridTest --consumption-plan-location westeurope

az functionapp deployment source config --repo-url https://github.com/olandese/EventGrid --branch master --manual-integration --resource-group EventGridTest --name <app_name> 

azure ad sp create -n EventGridTestSP -p Q1w2e3e3r4t5y6

azure ad sp show -c EventGridTestSP --json

az role assignment create --assignee <appid> --role Contributor

az webapp config appsettings set -g EventGridTest -n storagencryptioncheck --settings ClientId=<appid> ClientSecret=Q1w2e3e3r4t5y6

az eventgrid event-subscription create --name CheckStorageAccountEncryption --included-event-types Microsoft.Resources.ResourceWriteSuccess --endpoint "<function_url>"

```