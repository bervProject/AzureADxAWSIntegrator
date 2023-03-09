using Microsoft.Graph;
using Microsoft.Graph.ApplicationTemplates.Item.Instantiate;
using Microsoft.Graph.Models;
using Microsoft.Graph.ServicePrincipals.Item.AddTokenSigningCertificate;

namespace AzureADIntegrator;

/// <summary>
/// Azure AD Setup SAML. Reference: https://learn.microsoft.com/en-us/graph/application-saml-sso-configure-api?tabs=http
/// </summary>
public class AzureAdSetup
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly HttpClient _httpClient;
    private const string AppNameTemplate = "AWS IAM Identity Center (successor to AWS Single Sign-On)";
    private const string AwsClaimsPolicyName = "AWS Claims Policy";

    /// <summary>
    /// Azure AD Setup Business Context
    /// </summary>
    /// <param name="graphServiceClient">GraphServiceClient</param>
    /// <param name="httpClient">HttpClient</param>
    public AzureAdSetup(GraphServiceClient graphServiceClient, HttpClient httpClient)
    {
        _graphServiceClient = graphServiceClient;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Setup Application Async
    /// </summary>
    /// <param name="applicationName">Application Name</param>
    /// <param name="appNumber">Application Number</param>
    /// <returns>Service Principal Object Id, Application Object Id, ClaimMappings Id, Application Id</returns>
    public async Task<(string servicePrincipalId, string appObjectId, string claimMappingId, string applicationId)> SetupApplicationAsync(string applicationName, string appNumber, string providerName)
    {
        // Initialize Application
        Console.WriteLine("Initialize Application");
        var (applicationId, servicePrincipalId, appId) = await InitializeApp(applicationName);
        if (string.IsNullOrEmpty(applicationId) || string.IsNullOrEmpty(servicePrincipalId) || string.IsNullOrEmpty(appId))
        {
            return (string.Empty, string.Empty, string.Empty, string.Empty);
        }
        // Configure SSO
        Console.WriteLine("Configure SSO");
        await ConfigureSso(applicationId, servicePrincipalId, appNumber);
        // Configure Roles
        Console.WriteLine("Configure Roles");
        var adminGuid = Guid.Parse("3a84e31e-bffa-470f-b9e6-754a61e4dc63");
        await ConfigureRoles(servicePrincipalId, adminGuid, providerName);
        // Setup claims mapping policy
        Console.WriteLine("Configure Claims Mapping Policies");
        var claimMappingsId = await SetupClaimsMappingPolicy(servicePrincipalId);
        // setup Signing Certificate
        Console.WriteLine("Configure Singing Certificates");
        await SetupSigningCertificate(servicePrincipalId, applicationName);
        // Optional - Setup User & Role
        Console.WriteLine("Configure User & Roles");
        await SetupUser(servicePrincipalId, adminGuid);
        return (servicePrincipalId, applicationId, claimMappingsId, appId);
    }

    private async Task SetupUser(string servicePrincipalId, Guid adminGuid)
    {
        // Optional - Create User
        var user = new User
        {
            AccountEnabled = true,
            DisplayName = "User Integrated with AWS 1",
            MailNickname = "IntegratedAWS1",
            UserPrincipalName = "aws1@berviantoleo.my.id",
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = true,
                Password = "TemporaryPass123==="
            }
        };

        var userResponse = await _graphServiceClient.Users.PostAsync(user);
        
        // Optional - App Assignment
        var appRoleAssignment = new AppRoleAssignment
        {
            PrincipalId = Guid.Parse(userResponse.Id), // user id
            PrincipalType = "User",
            AppRoleId = adminGuid,
            ResourceId = Guid.Parse(servicePrincipalId)
        };

        await _graphServiceClient.ServicePrincipals[servicePrincipalId].AppRoleAssignments.PostAsync(appRoleAssignment);
    }

    private async Task SetupSigningCertificate(string servicePrincipalId, string applicationName)
    {
        var displayName = $"CN={applicationName}";
        var endDateTime = DateTimeOffset.Parse("2024-01-25T00:00:00Z");
        var request = new AddTokenSigningCertificatePostRequestBody {
            DisplayName = displayName,
            EndDateTime = endDateTime
        };
        var tokenResponse = await _graphServiceClient.ServicePrincipals[servicePrincipalId].AddTokenSigningCertificate.PostAsync(request);
        var spRequest = new ServicePrincipal
        {
            PreferredTokenSigningKeyThumbprint = tokenResponse.Thumbprint
        };
        await _graphServiceClient.ServicePrincipals[servicePrincipalId].PatchAsync(spRequest);
    }

    private async Task<string> SetupClaimsMappingPolicy(string servicePrincipalId)
    {
        var policiesResponse = await _graphServiceClient.Policies.ClaimsMappingPolicies.GetAsync(
            requestConfig => {
                requestConfig.QueryParameters.Filter = $"DisplayName eq '{AwsClaimsPolicyName}'";
                requestConfig.QueryParameters.Top = 1;
            }
        );
        string mappingPoliciesId;
        var policies = policiesResponse?.Value;
        if (policies != null && policies.Count == 1)
        {
            // give existing
            mappingPoliciesId = policies[0].Id;
        }
        else
        {
            var claimsMappingPolicy = new ClaimsMappingPolicy
            {
                Definition = new List<String>()
                {
                    "{\"ClaimsMappingPolicy\":{\"Version\":1,\"IncludeBasicClaimSet\":\"true\", \"ClaimsSchema\": [{\"Source\":\"user\",\"ID\":\"assignedroles\",\"SamlClaimType\": \"https://aws.amazon.com/SAML/Attributes/Role\"}, {\"Source\":\"user\",\"ID\":\"userprincipalname\",\"SamlClaimType\": \"https://aws.amazon.com/SAML/Attributes/RoleSessionName\"}, {\"Value\":\"900\",\"SamlClaimType\": \"https://aws.amazon.com/SAML/Attributes/SessionDuration\"}, {\"Source\":\"user\",\"ID\":\"assignedroles\",\"SamlClaimType\": \"appRoles\"}, {\"Source\":\"user\",\"ID\":\"userprincipalname\",\"SamlClaimType\": \"https://aws.amazon.com/SAML/Attributes/nameidentifier\"}]}}"
                },
                DisplayName = AwsClaimsPolicyName,
                IsOrganizationDefault = false
            };
            var claimsMappingPolicyResponse = await _graphServiceClient.Policies.ClaimsMappingPolicies.PostAsync(claimsMappingPolicy);
            mappingPoliciesId = claimsMappingPolicyResponse.Id;
        }
        
        var claimsMappingPolicyReference = new ReferenceCreate
        {
            OdataId = $"https://graph.microsoft.com/v1.0/policies/claimsMappingPolicies/{mappingPoliciesId}"
        };
        await _graphServiceClient.ServicePrincipals[servicePrincipalId].ClaimsMappingPolicies.Ref.PostAsync(claimsMappingPolicyReference);
        return mappingPoliciesId;
    }

    private async Task ConfigureRoles(string servicePrincipalId, Guid adminGuid, string providerName)
    {
        var accountId = Environment.GetEnvironmentVariable("AWS_ACCOUNT_ID");
        // setup App Roles
        var servicePrincipal = new ServicePrincipal
        {
            AppRoles = new List<AppRole>()
            {
                new()
                {
                    AllowedMemberTypes = new List<String>()
                    {
                        "User"
                    },
                    DisplayName = "User",
                    Id = Guid.Parse("8774f594-1d59-4279-b9d9-59ef09a23530"),
                    IsEnabled = true,
                    Description = "User",
                    Value = null,
                    Origin = "Application"
                },
                new()
                {
                    AllowedMemberTypes = new List<String>()
                    {
                        "User"
                    },
                    DisplayName = "msiam_access",
                    Id = Guid.Parse("e7f1a7f3-9eda-48e0-9963-bd67bf531afd"),
                    IsEnabled = true,
                    Description = "msiam_access",
                    Value = null,
                    Origin = "Application"
                },
                new()
                {
                    AllowedMemberTypes = new List<String>()
                    {
                        "User"
                    },
                    Description = "Admin (AWS) Readonly",
                    DisplayName = "Admin,AWS,Readonly",
                    Id = adminGuid,
                    IsEnabled = true,
                    Value =
                        $"arn:aws:iam::{accountId}:role/AADReadonly,arn:aws:iam::{accountId}:saml-provider/{providerName}"
                },
            }
        };
        await _graphServiceClient.ServicePrincipals[servicePrincipalId].PatchAsync(servicePrincipal);
    }

    private async Task ConfigureSso(string applicationId, string servicePrincipalId, string appNumber)
    {
        // Configure SSO
        var updatedSp = new ServicePrincipal()
        {
            PreferredSingleSignOnMode = "saml"
        };
        await _graphServiceClient.ServicePrincipals[servicePrincipalId].PatchAsync(updatedSp);
        // setup basic SAML URLs
        var application = new Application
        {
            Web = new WebApplication
            {
                RedirectUris = new List<String>()
                {
                    "https://signin.aws.amazon.com/saml"
                }
            },
            IdentifierUris = new List<String>()
            {
                $"https://signin.aws.amazon.com/saml#{appNumber}"
            }
        };
        await _graphServiceClient.Applications[applicationId].PatchAsync(application);
    }

    private async Task<(string applicationId, string servicePrincipalId, string appId)> InitializeApp(string applicationName)
    {
        // Get Template Id
        var templatesResponse = await _graphServiceClient.ApplicationTemplates
            .GetAsync(requestConfig => {
                requestConfig.QueryParameters.Filter = $"displayName eq '{AppNameTemplate}'";
                requestConfig.QueryParameters.Top = 1;
            });
        var templates = templatesResponse?.Value;
        if (templates != null && templates.Count != 1)
        {
            return (string.Empty, string.Empty, string.Empty);
        }
        var template = templates[0];
        // create app
        var request = new InstantiatePostRequestBody {
            DisplayName = applicationName
        };
        var response = await _graphServiceClient.ApplicationTemplates[template.Id].Instantiate.PostAsync(request);
        // wait for 60s
        await Task.Delay(TimeSpan.FromSeconds(60));
        return (response.Application.Id, response.ServicePrincipal.Id, response.ServicePrincipal.AppId);
    }

    /// <summary>
    /// Download SAML Async
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="appId"></param>
    public async Task<Stream?> DownloadSamlAsync(string tenantId, string appId)
    {
        var response = await _httpClient.GetAsync($"https://login.microsoftonline.com/{tenantId}/federationmetadata/2007-06/federationmetadata.xml?appid={appId}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadAsStreamAsync();
    }
    
    /// <summary>
    /// Download SAML Async
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="appId"></param>
    public async Task<string> DownloadSamlStringAsync(string tenantId, string appId)
    {
        var response = await _httpClient.GetAsync($"https://login.microsoftonline.com/{tenantId}/federationmetadata/2007-06/federationmetadata.xml?appid={appId}");
        if (!response.IsSuccessStatusCode)
        {
            return string.Empty;
        }
        return await response.Content.ReadAsStringAsync();
    }
}