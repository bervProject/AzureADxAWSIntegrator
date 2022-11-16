using Amazon.IdentityManagement;
using AWSIntegrator;
using Azure.Identity;
using AzureADAutomate.Models;
using AzureADIntegrator;
using Microsoft.Graph;
using Redis.OM;

// Read Env/Config
var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
{
    return;
}

// Declaration
RedisConnectionProvider? cacheClient = null;
AzureAdSetup? azureAdSetup = null;
AwsIamSetup? awsIntegrator = null;

// Constants Config
string[] scopes = { "https://graph.microsoft.com/.default" };
const string providerName = "AzureAdAutomate";

// Setup
async Task SetupAsync()
{
    // Setup Cache Client    
    cacheClient = new RedisConnectionProvider("redis://localhost:6379");
    await cacheClient.Connection.CreateIndexAsync(typeof(AppCache));

    // Setup Graph API Client
    var clientSecretCredential = new ClientSecretCredential(
        tenantId, clientId, clientSecret);
    var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
    // Setup Azure AD Business Process
    var httpClient = new HttpClient();
    azureAdSetup = new AzureAdSetup(graphClient, httpClient);

    // Setup AWS Dependencies
    var identityClient = new AmazonIdentityManagementServiceClient();
    awsIntegrator = new AwsIamSetup(identityClient);
}

// Setup
await SetupAsync();
// Stop if didn't initialize
if (cacheClient == null || azureAdSetup == null || awsIntegrator == null)
{
    return;
}

// check cache
var collections = cacheClient.RedisCollection<AppCache>();
var cacheData = collections.FirstOrDefault();

// obtain appId
string appIdResult;
if (cacheData != null)
{
    appIdResult = cacheData.AppId;
}
else
{
    // create app & user
    var (spId, applicationId, claimMappingId, appId) =
        await azureAdSetup.SetupApplicationAsync($"AWS-1-{Guid.NewGuid()}", "1", providerName);
    collections.Insert(new AppCache()
    {
        ApplicationId = applicationId,
        ServicePrincipalId = spId,
        ClaimMappingsId = claimMappingId,
        AppId = appId
    });
    appIdResult = appId;
}

// download SAML
var samlString = await azureAdSetup.DownloadSamlStringAsync(tenantId, appIdResult);
if (samlString == string.Empty)
{
    // stop
    return;
}

// setup SAML to AWS
await awsIntegrator.SetupRole(samlString, providerName);