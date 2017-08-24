# EventGrid

1. Login in the Azure Portal and start the Azure Cloud Shell

2. Create a new Resource Group

    ```azurecli-interactive
    az group create --name EventGridTest --location westeurope
    ```
3. Create a new Storage Account, replace <storage_name> with a unique name: 

    ```azurecli-interactive
    az storage account create --name <storage_name> --location westeurope --resource-group EventGridTest --sku Standard_LRS
    ```

4. Create a new Function App, replace <app_name> with a unique name and <storage_name> with the one used in step 3

    ```azurecli-interactive
    az functionapp create --name <app_name> --storage-account <storage_name> --resource-group EventGridTest --consumption-plan-location westeurope
    ```

5. Create an automatic deployment to the function app, replace <app_name> with the one used in step 4

    ```azurecli-interactive
    az functionapp deployment source config --repo-url https://github.com/olandese/EventGrid --branch master --manual-integration --resource-group EventGridTest --name <app_name> 
    ```

6. Create a new Service Principal

    ```azurecli-interactive
    azure ad sp create -n EventGridTestSP -p Q1w2e3e3r4t5y6
    ```
7. Copy the appid from the output of step 6 (see the picture below)
    ![Appid](https://raw.githubusercontent.com/olandese/EventGrid/master/img/principalappid.PNG)

8. Make the Service Principal Contributor on the subscription, replace <appid> with the value from step 7

    ```azurecli-interactive
    az role assignment create --role Contributor --assignee <appid>
    ```
9. Save settings for the function, replace <app_name> with with the one used in step 4 and <appid> with appid of step 7

    ```azurecli-interactive
    az webapp config appsettings set -g EventGridTest -n <app_name> --settings ClientSecret=Q1w2e3e3r4t5y6 ClientId=<appid>
    ```
10. Create an Event Grid subscription for all successful deployments and the handler will be the function, replace <app_name> with with the one used in step 4

    ```azurecli-interactive
    az eventgrid event-subscription create --name CheckStorageAccountEncryption --included-event-types Microsoft.Resources.ResourceWriteSuccess --endpoint "https://<app_name>.azurewebsites.net/api/HttpTriggerCheckStorageEncryption"
    ``` 

11. Now create in your subscription some Storage Account, in the function monitor output you will see if they are created with Ecnryption or not.