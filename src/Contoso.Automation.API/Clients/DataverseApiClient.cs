using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Contoso.Automation.API.Auth;
using Contoso.Automation.Core.Configuration;
using RestSharp;
using Serilog;

namespace Contoso.Automation.API.Clients;

/// <summary>
/// Generic Dataverse Web API client implementing OData v4 CRUD operations.
/// Used both for test data setup (seeding) and backend validation (asserting
/// what was actually persisted, not just what the UI shows).
///
/// Dataverse OData conventions:
///   GET    /api/data/v9.2/{entities}({id})
///   POST   /api/data/v9.2/{entities}        → returns 204 + OData-EntityId header
///   PATCH  /api/data/v9.2/{entities}({id})  → returns 204
///   DELETE /api/data/v9.2/{entities}({id})  → returns 204
///   GET    /api/data/v9.2/{entities}?$select=...&$filter=...
///
/// Entity plural name examples: accounts, contacts, opportunities
/// </summary>
public sealed class DataverseApiClient
{
    private readonly RestClient _client;
    private readonly DataverseAuthClient _authClient;
    private readonly ILogger _log = Log.ForContext<DataverseApiClient>();

    public DataverseApiClient(TestConfiguration config, DataverseAuthClient authClient)
    {
        _authClient = authClient;
        _client = new RestClient(new RestClientOptions(config.Dataverse.ApiBaseUrl)
        {
            ThrowOnAnyError   = false,
            MaxTimeout        = 30_000,
        });
    }

    /// <summary>
    /// Creates a new record and returns its GUID.
    /// entityData should be a serialisable object or Dictionary matching the OData body.
    /// </summary>
    public async Task<Guid> CreateAsync(string entityPluralName, object entityData)
    {
        var request = await BuildRequestAsync(Method.Post, entityPluralName);
        request.AddJsonBody(entityData);

        var response = await _client.ExecuteAsync(request);
        EnsureSuccess(response, $"Create {entityPluralName}");

        // Dataverse returns the new record URL in the OData-EntityId header
        var entityIdHeader = response.Headers?
            .FirstOrDefault(h => h.Name?.Equals("OData-EntityId", StringComparison.OrdinalIgnoreCase) == true);

        if (entityIdHeader?.Value?.ToString() is string headerValue)
        {
            var guidMatch = System.Text.RegularExpressions.Regex.Match(headerValue, @"\(([0-9a-fA-F\-]{36})\)");
            if (guidMatch.Success)
            {
                var id = Guid.Parse(guidMatch.Groups[1].Value);
                _log.Debug("Created {Entity} with ID {Id}", entityPluralName, id);
                return id;
            }
        }

        throw new InvalidOperationException($"Could not extract record ID from Dataverse response for {entityPluralName}");
    }

    /// <summary>
    /// Retrieves a single record by its GUID. Returns null if not found.
    /// </summary>
    public async Task<T?> GetByIdAsync<T>(string entityPluralName, Guid id, string? select = null) where T : class
    {
        var path = $"{entityPluralName}({id:D})";
        if (!string.IsNullOrEmpty(select))
            path += $"?$select={select}";

        var request = await BuildRequestAsync(Method.Get, path);
        var response = await _client.ExecuteAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        EnsureSuccess(response, $"Get {entityPluralName}({id})");
        return JsonConvert.DeserializeObject<T>(response.Content!);
    }

    /// <summary>
    /// Queries records with OData $filter and optional $select/$top.
    /// Returns an empty list if no matches.
    /// </summary>
    public async Task<List<T>> QueryAsync<T>(
        string entityPluralName,
        string? filter = null,
        string? select = null,
        int? top = null) where T : class
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrEmpty(filter)) queryParts.Add($"$filter={Uri.EscapeDataString(filter)}");
        if (!string.IsNullOrEmpty(select)) queryParts.Add($"$select={select}");
        if (top.HasValue)                  queryParts.Add($"$top={top}");

        var path = entityPluralName;
        if (queryParts.Any())
            path += "?" + string.Join("&", queryParts);

        var request = await BuildRequestAsync(Method.Get, path);
        var response = await _client.ExecuteAsync(request);
        EnsureSuccess(response, $"Query {entityPluralName}");

        var json  = JObject.Parse(response.Content!);
        var items = json["value"]?.ToObject<List<T>>() ?? new List<T>();

        _log.Debug("Query {Entity} returned {Count} records", entityPluralName, items.Count);
        return items;
    }

    /// <summary>
    /// Updates specific fields on an existing record using PATCH.
    /// Only the properties in patchData are updated; others are untouched.
    /// </summary>
    public async Task UpdateAsync(string entityPluralName, Guid id, object patchData)
    {
        var request = await BuildRequestAsync(Method.Patch, $"{entityPluralName}({id:D})");
        request.AddJsonBody(patchData);

        var response = await _client.ExecuteAsync(request);
        EnsureSuccess(response, $"Update {entityPluralName}({id})");
        _log.Debug("Updated {Entity}({Id})", entityPluralName, id);
    }

    /// <summary>
    /// Permanently deletes a record. Does not throw if the record is already gone (idempotent).
    /// </summary>
    public async Task DeleteAsync(string entityPluralName, Guid id)
    {
        var request = await BuildRequestAsync(Method.Delete, $"{entityPluralName}({id:D})");
        var response = await _client.ExecuteAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _log.Debug("{Entity}({Id}) already deleted - skipping", entityPluralName, id);
            return;
        }

        EnsureSuccess(response, $"Delete {entityPluralName}({id})");
        _log.Information("Deleted {Entity}({Id})", entityPluralName, id);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<RestRequest> BuildRequestAsync(Method method, string resource)
    {
        var token = await _authClient.GetAccessTokenAsync();

        var request = new RestRequest(resource, method);
        request.AddHeader("Authorization", $"Bearer {token}");
        request.AddHeader("OData-MaxVersion", "4.0");
        request.AddHeader("OData-Version", "4.0");
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Content-Type", "application/json; charset=utf-8");
        request.AddHeader("Prefer", "return=representation");

        return request;
    }

    private static void EnsureSuccess(RestResponse response, string operation)
    {
        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            return;

        throw new HttpRequestException(
            $"Dataverse API error on {operation}. " +
            $"Status: {(int)response.StatusCode} {response.StatusCode}. " +
            $"Body: {response.Content}");
    }
}
