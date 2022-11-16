# Azure AD Integrator

Library to handle Microsoft Graph SDK.

## Used Resources

- ApplicationTemplates
- ServicePrincipals
  - AppRoleAssignments
  - AddTokenSigningCertificate
  - ClaimsMappingPolicies
- Applications
- Policies
  - ClaimsMappingPolicies

## Flows

```mermaid
flowchart TD
    a[Get template id] --> b{Is Exist?}
    b -->|Yes| c[Create app using template id]
    b -->|No| d[End]
    c --> e
    e[Getting Roles] --> f[Update Service Principal SignOn Mode]
    f --> g[Update Application RedirectUris & IdentifierUris]
    g --> h[Update Service Principal App Roles] 
    h --> i[Getting Claim Mapping Policies]
    i --> j{Is Exist}
    j -->|No| k[Create Claim Mapping Policies]
    j -->|Yes| l[Assign the Claim Mapping Policies into Service Principal]
    k --> l
    l --> m[Create Signing Certificate for SP]
    m --> n[Activate Singing Certificate for SP]
    n --> o[Create User]
    o --> p[Assign User to SP with specific role]
    p --> q[End]
```

## License

MIT