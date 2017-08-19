using System;
using System.Configuration;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace EventGridDemo
{
    public static class StorageAccountEncryptionChecker
    {
        [FunctionName("StorageAccountEncryptionChecker")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // Get request body
            string data = await req.Content.ReadAsStringAsync();

            // Parse into a dynamic object
            JArray array = JArray.Parse(data);
            var expConverter = new ExpandoObjectConverter();
            dynamic eventData = JsonConvert.DeserializeObject<ExpandoObject>(array[0].ToString(), expConverter);

            // We only want to process events when a Storage Account is created successfully 
            if (eventData.data.resourceProvider != "Microsoft.Storage" || 
                eventData.eventType != "Microsoft.Resources.ResourceWriteSuccess")
            {
                log.Info("Not an Azure Storage Resource");
                return req.CreateResponse(HttpStatusCode.OK, "Not an Azure Storage Resource");
            }

            string subscriptionID = eventData.data.subscriptionId;
            string tenantID = eventData.data.tenantId;

            ServicePrincipalLoginInformation sp = new ServicePrincipalLoginInformation();

            sp.ClientId = ConfigurationManager.AppSettings["ClientId"];
            sp.ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];

            string storageAccountID = eventData.subject;

            AzureCredentials cred = new AzureCredentials(sp, tenantID, AzureEnvironment.AzureGlobalCloud);
            IAzure azure = Azure.Authenticate(cred).WithSubscription(subscriptionID);

            bool? isEncrypted = false;

            var storage = azure.StorageAccounts.GetById(storageAccountID);

            if (storage?.Encryption != null)
            {
                isEncrypted = storage.Encryption.Services.Blob.Enabled;
            }

            string returnText;

            if (isEncrypted.GetValueOrDefault())
            {
                returnText = "Storage Account Encrypted";
            }
            else
            {
                returnText = "Storage Account NOT Encrypted";
            }

            log.Info(returnText);

            // We always return OK status (200), otherwise the EventGrid is going to retry the call
            // see https://docs.microsoft.com/en-us/azure/event-grid/delivery-and-retry#retry-intervals
            // in our case we don't need this, we just want to log the result
            return req.CreateResponse(HttpStatusCode.OK, returnText);
        }
    }
}
