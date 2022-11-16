using Redis.OM.Modeling;

namespace AzureADAutomate.Models;

/// <summary>
/// App Cache Model
/// </summary>
[Document(StorageType = StorageType.Json)]
public class AppCache
{
    /// <summary>
    /// Application Object Id
    /// </summary>
    [RedisField]
    [Searchable]
    public string ApplicationId { get; set; } = default!;
    /// <summary>
    /// Service Principal Object Id
    /// </summary>
    [RedisField]
    [Searchable]
    public string ServicePrincipalId { get; set; } = default!;
    /// <summary>
    /// Claim Mappings Id
    /// </summary>
    [RedisField]
    [Searchable]
    public string ClaimMappingsId { get; set; } = default!;
    /// <summary>
    /// App Id
    /// </summary>
    [RedisField]
    [Searchable]
    public string AppId { get; set; } = default!;
}