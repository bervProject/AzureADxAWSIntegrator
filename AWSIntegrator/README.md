# AWS Integrator

Library to handle AWS SDK. Assuming the Policies is exists.

## Used Resources

- Identity Provider
- IAM Role

## Features

- Create Identity Provider
- Update Identity Provider
- Create IAM Role
- Update IAM Role

## Flows

```mermaid
flowchart TD
    a[Getting List of Identity Provider] --> b{Is Exist?}
    b -->|Yes| c[Update XML]
    b -->|No| d[Create Identity Provider]
    e[Getting Roles] --> f{Is Exist?}
    c --> e
    d --> e
    f -->|Yes| g[Update IAM Role to Use Identity Provider]
    f -->|No| h[Create IAM Role]
    g --> i[End] 
    h --> i
```

## License

MIT