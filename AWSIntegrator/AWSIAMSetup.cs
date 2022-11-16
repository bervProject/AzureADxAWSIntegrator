using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace AWSIntegrator;

/// <summary>
/// AWS IAM Setup
/// </summary>
public class AwsIamSetup
{
    private readonly AmazonIdentityManagementServiceClient _client;

    private const string Template =
        "{{\"Version\": \"2012-10-17\",\"Statement\": [{{\"Effect\": \"Allow\",\"Action\": \"sts:AssumeRoleWithSAML\",\"Principal\": {{\"Federated\": \"{0}\"}},\"Condition\": {{\"StringEquals\": {{\"SAML:aud\": [\"https://signin.aws.amazon.com/saml\"]}}}}}}]}}";

    /// <summary>
    /// AWS IAM Setup. Depend on AmazonIdentityManagementServiceClient.
    /// </summary>
    /// <param name="client">AmazonIdentityManagementServiceClient</param>
    public AwsIamSetup(AmazonIdentityManagementServiceClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Setup Role
    /// </summary>
    /// <param name="saml">SAML XML in string</param>
    /// <param name="providerName">Provider Name</param>
    public async Task SetupRole(string saml, string providerName)
    {
        Console.WriteLine("Configure SAML Providers");
        var samlArn = await SetupSamlProvider(providerName, saml);
        Console.WriteLine("Configure Role");
        await SetupRole(samlArn);
    }

    private async Task<string> SetupSamlProvider(string providerName, string saml)
    {
        var samlList = await _client.ListSAMLProvidersAsync();
        var existing = samlList.SAMLProviderList.FirstOrDefault(x => x.Arn.Contains(providerName));
        string samlArn;
        if (existing == null)
        {
            var provider = await _client.CreateSAMLProviderAsync(new CreateSAMLProviderRequest()
            {
                Name = providerName,
                SAMLMetadataDocument = saml
            });
            samlArn = provider.SAMLProviderArn;
        }
        else
        {
            await _client.UpdateSAMLProviderAsync(new UpdateSAMLProviderRequest()
            {
                SAMLProviderArn = existing.Arn,
                SAMLMetadataDocument = saml
            });
            samlArn = existing.Arn;
        }

        return samlArn;
    }

    private async Task SetupRole(string samlArn)
    {
        var accountId = Environment.GetEnvironmentVariable("AWS_ACCOUNT_ID");
        var roles = await _client.ListRolesAsync();
        var existingRole = roles.Roles.FirstOrDefault(x => x.Arn.Contains("AADReadonly"));
        if (existingRole == null)
        {
            await _client.CreateRoleAsync(new CreateRoleRequest()
            {
                AssumeRolePolicyDocument = string.Format(Template, samlArn),
                PermissionsBoundary = $"arn:aws:iam::{accountId}:policy/AAD_Account_ReadOnly",
                RoleName = "AADReadonly"
            });
        }

        await _client.AttachRolePolicyAsync(new AttachRolePolicyRequest()
        {
            RoleName = "AADReadonly",
            PolicyArn = $"arn:aws:iam::{accountId}:policy/AAD_Account_ReadOnly"
        });
    }
}