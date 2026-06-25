using Microsoft.Identity.Client;
using Contoso.Automation.Core.Configuration;
using Serilog;

namespace Contoso.Automation.API.Auth;

/// <summary>
/// Acquires OAuth2 bearer tokens for Dataverse Web API calls using the
/// Resource Owner Password Credentials (ROPC) flow.
///
/// ROPC flow: exchanges username + password for a bearer token directly,
/// without a browser redirect. This is the appropriate flow for automation
/// service accounts where interactive login is not possible.
///
/// Requirement: The Azure AD App Registration must have the
/// "Allow public client flows" setting enabled and have the
/// Dynamics CRM user_impersonation API permission consented.
///
/// Token caching: MSAL's PublicClientApplication handles token caching and
/// silent refresh automatically. Tokens are cached in memory for the process
/// lifetime and refreshed before expiry.
/// </summary>
public sealed class DataverseAuthClient
{
    private readonly DataverseSettings _settings;
    private readonly D365Settings _d365Settings;
    private readonly IPublicClientApplication _msalClient;
    private readonly ILogger _log = Log.ForContext<DataverseAuthClient>();
    private string[]? _scopes;

    public DataverseAuthClient(TestConfiguration config)
    {
        _settings    = config.Dataverse;
        _d365Settings = config.D365;

        _msalClient = PublicClientApplicationBuilder
            .Create(_settings.ClientId)
            .WithAuthority($"https://login.microsoftonline.com/{_settings.TenantId}")
            .Build();
    }

    /// <summary>
    /// Returns a valid bearer token, acquiring or refreshing as needed.
    /// First call performs ROPC login; subsequent calls return the cached token.
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        _scopes ??= new[] { $"{_settings.Resource}.default" };

        AuthenticationResult result;

        try
        {
            // Attempt silent token acquisition from MSAL cache first
            var accounts = await _msalClient.GetAccountsAsync();
            result = await _msalClient
                .AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                .ExecuteAsync();

            _log.Debug("Dataverse token served from MSAL cache");
        }
        catch (MsalUiRequiredException)
        {
            // Cache miss or token expired - perform ROPC acquisition
            _log.Information("Acquiring Dataverse token via ROPC for {User}", _d365Settings.Username);

            result = await _msalClient
                .AcquireTokenByUsernamePassword(
                    _scopes,
                    _d365Settings.Username,
                    _d365Settings.Password)
                .ExecuteAsync();

            _log.Debug("Dataverse ROPC token acquired, expires {Expiry}", result.ExpiresOn);
        }

        return result.AccessToken;
    }
}
