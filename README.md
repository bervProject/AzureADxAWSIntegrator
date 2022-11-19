# Azure AD SSO Automate - AWS

## Flow

### General Flow

```mermaid
flowchart LR
    subgraph azuread[Azure AD]
        aad1[Settings Enterprise Application] --> aad2[Download XML Federation]
    end
    subgraph aws[AWS]
        aad2 --> aws1[Settings Identity Provider]
        aws1 --> aws2[Add/Create Role]    
    end
```

### Flow (Technical Terms)

```mermaid
flowchart TD
    subgraph azuread[Azure AD]
        aad1[Create Enterprise App from Templates] --> aad2[Update Service Principal & App Registration to use SAML]
        aad2 --> aad3[Configure Service Principal Roles]
        aad3 --> aad4[Configure Claim Mapping Policies & Assign to Service Principal]
        aad4 --> aad5[Configure Singing Certificates for Service Principal]
        aad5 --> aad6[Optional - Configure User & Assign to a role]
    end
    subgraph aws[AWS]
        aad5 --> aws1[Add/Get SAML Provider]
        aws1 --> aws2[Create/Update Role to be assigned with SAML Provider]    
    end
```

## Setup Project

### Environment Variables

```bash
## For the Console App/Business Flow, planned to be removed
AWS_ACCOUNT_ID=
## For AWS SDK
AWS_PROFILE=
# For Azure AD / Microsoft Graph
CLIENT_ID=
CLIENT_SECRET=
TENANT_ID=
```

**Note/Details**:

1. Please see [here](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html) for configuring the AWS SDK Credentials.
   1. Currently, we only change Identity Provider & IAM Roles. Please make sure your credentials allow you to create/update it.
2. Please see [here](https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider) for configuration the Microsoft Graph SDK Credentials.
   1. Please make sure the credentials have these permissions: `Application.ReadWrite.All`, `AppRoleAssignment.ReadWrite.All`, `Policy.Read.All`, `Policy.ReadWrite.ApplicationConfiguration`, and `User.ReadWrite.All`

### Dependencies & Tools

1. [Redis Stack](https://redis.io/docs/stack/) - Storing temporary data and will be used for main database
   1. Recommend using [Redis Docker](https://redis.io/docs/stack/get-started/install/docker/)
2. [.NET 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
3. Your IDE
   1. I recommend using [Visual Studio](https://visualstudio.microsoft.com/) or [Rider](https://www.jetbrains.com/rider/).

### Run Project

```bash
dotnet run --project AzureADAutomate
```

## Version

Currently, this project is still in beta testing.

## Roadmap for Next Iteration

### Version 1

- [ ] Remove AWS Account ID from Environment Variables and will be based on input
- [ ] Consider existing Enterprise Apps and re-configure existing apps

### Version 2

- [ ] Communication through REST API & Event-Driven

## Presentation

- [Introduction - Case Study: Connecting Azure AD SSO with AWS](https://docs.google.com/presentation/d/1Ipis0cnnzbUQnikzyDZ5ZX2C3bE-jsFVLGk7co3Ttvo/edit?usp=sharing)

## Resources

### Main Resources

#### Azure AD (AAD)

- [Tutorials/Documentations from Microsoft](https://learn.microsoft.com/en-us/graph/application-saml-sso-configure-api?tabs=csharp)
- Permissions `Application.ReadWrite.All`, `AppRoleAssignment.ReadWrite.All`, `Policy.Read.All`, `Policy.ReadWrite.ApplicationConfiguration`, and `User.ReadWrite.All`.
- [App List Dashboard](https://myapps.microsoft.com/)

#### AWS

- [Simple cross-platform application using the AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/quick-start-s3-1-cross.html)
- [AmazonIdentityManagementServiceClient](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/IAM/TIAMServiceClient.html)
- [AmazonIdentityManagementServiceClient.CreateSAMLProvider](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/IAM/MIAMServiceCreateSAMLProviderCreateSAMLProviderRequest.html)
- [AmazonIdentityManagementServiceClient.AttachRolePolicy](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/IAM/MIAMServiceAttachRolePolicyAttachRolePolicyRequest.html)
- [AmazonIdentityManagementServiceClient.CreatePolicy](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/IAM/MIAMServiceCreatePolicyCreatePolicyRequest.html)

### Setup SSO Manually - Single Account

![Single Account](./images/azureadxaws.drawio.png)

- https://learn.microsoft.com/en-us/azure/active-directory/saas-apps/amazon-web-service-tutorial

### Setup SSO Manually - Multiple Accounts

![Multiple AWS Account](./images/azureadxaws-1.drawio.png)

- https://learn.microsoft.com/en-us/azure/active-directory/saas-apps/aws-multi-accounts-tutorial

### Another Topic - Provisioning

- https://learn.microsoft.com/en-us/azure/active-directory/app-provisioning/application-provisioning-configuration-api

### Another Code Samples

- https://learn.microsoft.com/en-us/samples/azure-samples/ms-identity-dotnetcore-galleryapp-management/automate-saml-based-sso-app-configuration-using-ms-graph-api-sdk-net/

### Tools/SDK Documentation

- [Microsoft Graph Client](https://learn.microsoft.com/en-us/graph/sdks/create-client?tabs=CS)
- [Authentication Provider for MS Graph](https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider)

## License

MIT