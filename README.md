# EventGrid

## Description 
In this demo, we use Event Grid to publish Azure Subscriptions events to an Azure Function.
</BR>
The Event Grid events will be filtered to be of "Microsoft.Resources.ResourceWriteSuccess" kind. 
</BR>
Whenever a Storage Account will be created the function will output if the storage account is using encryption or not. 

## Setup

1. Login in the Azure Portal and start the Azure Cloud Shell  

2. Set the following variables, replace `<storage_name>` and `<app_name>` with a unique name:

    ```azurecli-interactive
    rgName="EventGridTest"
    storageaccountName=<storage_name>
    appName=<app_name>
    ```

3. Create a new Resource Group:

    ```azurecli-interactive
    az group create --name $rgName --location westeurope
    ```
4. Create a new Storage Account:

    ```azurecli-interactive
    az storage account create --location westeurope --resource-group $rgName --sku Standard_LRS --name $storageaccountName
    ```

5. Create a new Function App:

    ```azurecli-interactive
    az functionapp create --resource-group $rgName --consumption-plan-location westeurope --name $appName --storage-account $storageaccountName
    ```

6. Create an automatic deployment to the function app:

    ```azurecli-interactive
    az functionapp deployment source config --repo-url https://github.com/olandese/EventGrid --branch master --manual-integration --resource-group $rgName --name $appName 
    ```

7. Create a new Service Principal and make it Contributor on the subscription:

    ```azurecli-interactive
    spId="$(az ad sp create-for-rbac -n "EventGridTestSP" --role contributor --password Q1w2e3r4t5y6 --query "[appId] | [0]" --output tsv)"
    ```
8. Save the Service Principal values as settings for the function:

    ```azurecli-interactive
    az webapp config appsettings set -g EventGridTest --name $appName --settings ClientSecret=Q1w2e3r4t5y6 ClientId=$spId 
    ```
9. Create an Event Grid subscription for all successful deployments and the handler will be the function:

    ```azurecli-interactive
    az eventgrid event-subscription create --name CheckStorageAccountEncryption --included-event-types Microsoft.Resources.ResourceWriteSuccess --endpoint "https://$appName.azurewebsites.net/api/HttpTriggerCheckStorageEncryption"
    ``` 

10. Now create in your subscription some Storage Accounts, in the function monitor output you will see if they are created with Encryption or not:

    ```azurecli-interactive
    az storage account create --resource-group $rgName --encryption blob --sku Standard_LRS --name encrypttest

    az storage account create --resource-group $rgName --sku Standard_LRS --name notencrypttest    
    ``` 
## Cleanup 

If you want to cleanup all the resources created during the previous steps:

1. Delete the Resource Group

    ```azurecli-interactive
    az group delete --name EventGridTest --yes
    ``` 

2. Delete the Event Grid Subscription

    ```azurecli-interactive
    az eventgrid event-subscription delete --name CheckStorageAccountEncryption
    ``` 

3. Delete the Service Principal

    ```azurecli-interactive
    az ad sp delete --id "http://EventGridTestSP"
    ``` 
