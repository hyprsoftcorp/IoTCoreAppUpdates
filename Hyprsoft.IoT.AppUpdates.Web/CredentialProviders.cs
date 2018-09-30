using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public interface ICredentialProvider
    {
        Task<string> GetUsernameAsync();

        Task<string> GetPasswordAsync();
    }

    public abstract class CredentialProviderBase : ICredentialProvider
    {
        public CredentialProviderBase(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; private set; }

        public abstract Task<string> GetUsernameAsync();

        public abstract Task<string> GetPasswordAsync();
    }

    public class DefaultCredentialProvider : CredentialProviderBase
    {
        public DefaultCredentialProvider(IConfiguration configuration) : base(configuration) { }

        public override Task<string> GetUsernameAsync() => Task.FromResult(AuthenticationHelper.DefaultUsername);

        public override Task<string> GetPasswordAsync() => Task.FromResult(AuthenticationHelper.DefaultPassword);
    }

    public class AppSettingsCredentialProvider : CredentialProviderBase
    {
        public AppSettingsCredentialProvider(IConfiguration configuration) : base(configuration) { }

        public override Task<string> GetUsernameAsync() => Task.FromResult(Configuration["AppUpdatesUsername"]);

        public override Task<string> GetPasswordAsync() => Task.FromResult(Configuration["AppUpdatesPassword"]);
    }

    public class AzureKeyVaultCredentialProvider : CredentialProviderBase
    {
        #region Fields

        private KeyVaultClient _client;

        #endregion

        #region Constructors

        public AzureKeyVaultCredentialProvider(IConfiguration configuration) : base(configuration) { }

        #endregion

        #region Methods

        public async override Task<string> GetUsernameAsync() => await GetSecretFromKeyVaultClient(Configuration["AppUpdatesKeyVaultUsernameSecret"]);

        public async override Task<string> GetPasswordAsync() => await GetSecretFromKeyVaultClient(Configuration["AppUpdatesKeyVaultPasswordSecret"]);

        private async Task<string> GetSecretFromKeyVaultClient(string secretId)
        {
            if (_client == null)
            {
                if (!String.IsNullOrEmpty(Configuration["AppUpdatesKeyVaultClientId"]) &&
                    !String.IsNullOrEmpty(Configuration["AppUpdatesKeyVaultClientSecret"]) &&
                    !String.IsNullOrEmpty(Configuration["AppUpdatesKeyVaultUsernameSecret"]) &&
                    !String.IsNullOrEmpty(Configuration["AppUpdatesKeyVaultPasswordSecret"]))
                {
                    _client = new KeyVaultClient(async (authority, resource, scope) =>
                    {
                        var authContext = new AuthenticationContext(authority);
                        var credentials = new ClientCredential(Configuration["AppUpdatesKeyVaultClientId"], Configuration["AppUpdatesKeyVaultClientSecret"]);
                        var token = await authContext.AcquireTokenAsync(resource, credentials);
                        return token.AccessToken;
                    });
                }   // valid configuration?
                else
                    return null;
            }   // client null?
            return (await _client.GetSecretAsync(secretId)).Value;
        }

        #endregion
    }

    public class CredentialProviderHelper
    {
        #region Fields

        private readonly IConfiguration _configuration;

        #endregion

        #region Constructors

        public CredentialProviderHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #endregion

        #region Methods

        public async Task<ICredentialProvider> CreateProviderAsync()
        {
            ICredentialProvider provider = new AzureKeyVaultCredentialProvider(_configuration);
            if (!String.IsNullOrWhiteSpace(await provider.GetUsernameAsync()) && !String.IsNullOrEmpty(await provider.GetPasswordAsync()))
                return provider;

            provider = new AppSettingsCredentialProvider(_configuration);
            if (!String.IsNullOrWhiteSpace(await provider.GetUsernameAsync()) && !String.IsNullOrEmpty(await provider.GetPasswordAsync()))
                return provider;

            return new DefaultCredentialProvider(_configuration);
        }

        #endregion
    }
}
