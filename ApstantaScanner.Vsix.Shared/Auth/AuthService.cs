using GitHub.Authentication.CredentialManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace ApstantaScanner.Vsix.Shared.Auth
{
    public interface IAuthService
    {
        public Task InitiateDeviceFlowAsync();
    }

    public class AuthService : IAuthService
    {
        private const string CredStorageKey = "apsanta_user_key";
        private const string CredStorageUser = "apsanta_user";
        private const string ClientId = "Iv1.c51720e62268aece";        

        private AuthStatus currentAuthStatus = AuthStatus.NotStarted;
        private AuthStatus previousAuthStatus = AuthStatus.NotStarted;
        
        public AuthStatus AuthStatus
        {
            get => currentAuthStatus;
            set
            {
                if (currentAuthStatus != value)
                {
                    previousAuthStatus = currentAuthStatus;
                    currentAuthStatus = value;

                    GithubAuthStatusChanged?.Invoke(this, new GithubAuthStatusChangedEventArgs(previousAuthStatus, currentAuthStatus, UserCode, UserToken, ErrorMessage));
                }
            }
        }

        public string UserCode { get; private set; }
        public string UserToken { get; private set; }

        public string ErrorMessage{ get; private set; }

        public event EventHandler<GithubAuthStatusChangedEventArgs> GithubAuthStatusChanged;


        public AuthService()
        {
            LoadSavedCredentials();
        }

        public void LoadSavedCredentials()
        {
            var credential = Credential.Load(CredStorageKey);
            if (credential != null)
                UserToken = credential.Password;
        }

        private void CleanVariables()
        {
            currentAuthStatus = AuthStatus.NotStarted;
            previousAuthStatus = AuthStatus.NotStarted;
            UserToken = "";
            UserCode = "";
            ErrorMessage = "";
        }

        public async Task InitiateDeviceFlowAsync()
        {
            CleanVariables();

            using (HttpClient client = new())
            {
                var authCodeResponse = await StartDeviceFlowAsync(client);

                if (!string.IsNullOrWhiteSpace(authCodeResponse.DeviceCode))
                {
                    // verification url is partially trusted, add extra validation here to prevent a possible command injection
                    OpenWebPage(authCodeResponse.VerificationUri);

                    var authTokenResponse = await GetTokenAsync(client, authCodeResponse);

                    if (string.IsNullOrWhiteSpace(authTokenResponse.Error))
                    {
                        SaveCredentials(authTokenResponse);

                    }
                }
                
            } 
        }

        public void SaveCredentials(TokenResponse authTokenResponse)
        { 
            // save credentials
            Credential.Save(CredStorageKey, CredStorageUser, authTokenResponse.AccessToken);
        }



        public static void OpenWebPage(string url)
        {
            // validate url
            var psi = new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true };  

            Process.Start(psi);
        }

        public async Task<DeviceAuthorizationResponse> StartDeviceFlowAsync(HttpClient client)
        {            
            string deviceEndpoint = $"https://github.com/login/device/code";
            var request = new HttpRequestMessage(HttpMethod.Post, deviceEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = ClientId,
                    ["scope"] = "user, user:email"
                })
            };
            request.Headers.Add("Accept", "application/json");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var daResponseString = await response.Content.ReadAsStringAsync();
            //var json = JsonSerializer.Deserialize<DeviceAuthorizationResponse>(daResponseString);
            var authResponse = JsonConvert.DeserializeObject<DeviceAuthorizationResponse>(daResponseString);
            
            UserCode = authResponse.UserCode;
            AuthStatus = AuthStatus.DeviceCodeReceived;
            
            return authResponse;
        }

        public async Task<TokenResponse> GetTokenAsync(HttpClient client, DeviceAuthorizationResponse authResponse)
        {            
            string tokenEndpoint = "https://github.com/login/oauth/access_token";
            
            // Poll until we get a valid token response or a fatal error
            int pollingDelay = authResponse.Interval;
            while (true)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                        ["device_code"] = authResponse.DeviceCode,
                        ["client_id"] = ClientId
                    })
                };
                request.Headers.Add("Accept", "application/json");
                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    // Text.JSON
                    // var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);
                    if (tokenResponse?.Error == null)
                    {
                        UserToken = tokenResponse.AccessToken;
                        AuthStatus = AuthStatus.TokenReceived;
                       
                        return tokenResponse;
                    }

                    switch (tokenResponse.Error)
                    {
                        case "authorization_pending":
                            // Not complete yet, wait and try again later
                            AuthStatus = AuthStatus.AuthorizationPending;
                            break;
                        case "slow_down":
                            // Not complete yet, and we should slow down the polling
                            pollingDelay += 5;
                            break;
                        default:
                            // Some other error, nothing we can do but throw
                            AuthStatus = AuthStatus.Error;
                            ErrorMessage = $"Authorization failed: {tokenResponse.Error} - {tokenResponse.ErrorDescription}";
                            break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(pollingDelay));
                }
            }
        }
    }

    public enum AuthStatus
    {
        NotStarted = 0,
        DeviceCodeReceived,
        AuthorizationPending,
        TokenReceived,
        Error
    }
}
