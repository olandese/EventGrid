using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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

            string subscriptionID =  eventData.data.subscriptionId;
            string tenantID = eventData.data.tenantId;

            ServicePrincipalLoginInformation sp = new ServicePrincipalLoginInformation();

            sp.ClientId = "";
            sp.ClientSecret = "";

            string storageAccountID = eventData.subject;

            AzureCredentials cred = new AzureCredentials(sp, tenantID, AzureEnvironment.AzureGlobalCloud);
            IAzure azure = Azure.Authenticate(cred).WithSubscription(subscriptionID);

            bool? isEncrypted = false;

            var storage = azure.StorageAccounts.GetById(storageAccountID);

            if (storage?.Encryption != null)
            {
                isEncrypted = storage.Encryption.Services.Blob.Enabled;
            }

            return isEncrypted.GetValueOrDefault()
                ? req.CreateResponse(HttpStatusCode.OK, "Storage Account Encrypted")
                : req.CreateResponse(HttpStatusCode.BadRequest, "Storage Account Not Encrypted");
        }
    }
}
