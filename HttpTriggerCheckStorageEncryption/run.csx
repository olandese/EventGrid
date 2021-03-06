#r "Newtonsoft.Json" 

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    // Get request body
    string data = await req.Content.ReadAsStringAsync();

    //log.Info($"Content: {data}");
    
    // Parse into a dynamic object
    JArray array = JArray.Parse(data);
    var expConverter = new ExpandoObjectConverter();
    dynamic eventData = JsonConvert.DeserializeObject<ExpandoObject>(array[0].ToString(), expConverter);

    if (!IsPropertyExist(eventData, "data"))
    {
        log.Info("No data Property found");
        return req.CreateResponse(HttpStatusCode.OK, "No data Property found");
    }
    // We only want to process events when a Storage Account is created successfully 
    if (eventData.data.resourceProvider != "Microsoft.Storage")
    {
        log.Info("Not an Azure Storage Resource, not interested");
        return req.CreateResponse(HttpStatusCode.OK, "Not an Azure Storage Resource");
    }

    string subscriptionID = eventData.data.subscriptionId;
    string tenantID = eventData.data.tenantId;

    // Let's parse the resourceId information into a Dictionary
    string subject = eventData.subject.ToString();
    Dictionary<string, string> subjectDictionary = subject.ParseAzureResourceId();

    // Login using a Service Principal
    ServicePrincipalLoginInformation sp =
        new ServicePrincipalLoginInformation
        {
            ClientId = ConfigurationManager.AppSettings["ClientId"],
            ClientSecret = ConfigurationManager.AppSettings["ClientSecret"]
        };

    AzureCredentials cred = new AzureCredentials(sp, tenantID, AzureEnvironment.AzureGlobalCloud);
    IAzure azure = Azure.Authenticate(cred).WithSubscription(subscriptionID);

    string storageAccountID = eventData.subject;
    string storageAccountName = subjectDictionary["storageAccounts"];

    bool? isEncrypted = false;

    //Get the storage account
    var storage = azure.StorageAccounts.GetById(storageAccountID);

    //The Storage Account may already be deleted
    if (storage == null)
    {
        log.Info($"Storage Account {storageAccountName} was not found!");
        return req.CreateResponse(HttpStatusCode.OK, $"Storage Account {storageAccountName} was not found! ");
    }

    //And now let's check if the Blob encryption is enabled
    if (storage.Encryption != null)
    {
        isEncrypted = storage.Encryption.Services.Blob.Enabled;
    }

    string returnText;

    if (isEncrypted.GetValueOrDefault())
    {
        returnText = $"Storage Account {storageAccountName} is Encrypted";
    }
    else
    {
        returnText = $"Storage Account {storageAccountName} is NOT Encrypted"; ;
    }

    log.Info(returnText);

    // We always return OK status (200), otherwise the EventGrid is going to retry the call
    // see https://docs.microsoft.com/en-us/azure/event-grid/delivery-and-retry#retry-intervals
    // in our case we don't need this, we just want to log the result
    return req.CreateResponse(HttpStatusCode.OK, returnText);
}

private static Dictionary<string, string> ParseAzureResourceId(this string resourceID)
{
    return resourceID.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
        .Select((value, index) => new { Index = index, Value = value }).GroupBy(x => x.Index / 2).Select(
            g => new Tuple<string, string>(g.ElementAt(0).Value,
                g.ElementAt(1).Value)).ToDictionary(x => x.Item1, x => x.Item2);
}

public static bool IsPropertyExist(dynamic settings, string name)
{
    if (settings is ExpandoObject)
        return ((IDictionary<string, object>)settings).ContainsKey(name);

    return settings.GetType().GetProperty(name) != null;
}
