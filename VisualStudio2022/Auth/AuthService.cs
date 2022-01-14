using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace VisualStudio2022.Auth
{
    public class AuthService
    {
        private const string TenantId = "<your tenant id>";
        private const string ClientId = "Iv1.c51720e62268aece";

        //{"access_token":"...","token_type":"bearer","scope":""}

        public static async Task Main( )
        {
            Console.WriteLine("Hello World!");
            using var client = new HttpClient();
            var authorizationResponse = await StartDeviceFlowAsync(client);
            Console.WriteLine("Please visit this URL: " + authorizationResponse.VerificationUri);
            Console.WriteLine("And enter the following code: " + authorizationResponse.UserCode);
            OpenWebPage(authorizationResponse.VerificationUri);
            var tokenResponse = await GetTokenAsync(client, authorizationResponse);
            Console.WriteLine("Access token: ");
            Console.WriteLine(tokenResponse.AccessToken);
            Console.WriteLine("ID token: ");
            Console.WriteLine(tokenResponse.IdToken);
            Console.WriteLine("Refresh token: ");
            Console.WriteLine(tokenResponse.RefreshToken);
            Console.ReadLine();
        }

        private static void OpenWebPage(string url)
        {
            var psi = new ProcessStartInfo(url)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private static async Task<DeviceAuthorizationResponse> StartDeviceFlowAsync(HttpClient client)
        {
            string deviceEndpointAAD = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/devicecode";
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
            var json = JsonSerializer.Deserialize<DeviceAuthorizationResponse>(daResponseString);

            return json;
        }

        private static async Task<TokenResponse> GetTokenAsync(HttpClient client,
            DeviceAuthorizationResponse authResponse)
        {
            string tokenEndpointAAD = $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/token";
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
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
                    if (tokenResponse?.Error == null)
                    {
                        return tokenResponse;
                    }

                    switch (tokenResponse.Error)
                    {
                        case "authorization_pending":
                            // Not complete yet, wait and try again later
                            break;
                        case "slow_down":
                            // Not complete yet, and we should slow down the polling
                            pollingDelay += 5;
                            break;
                        default:
                            // Some other error, nothing we can do but throw
                            throw new Exception(
                                $"Authorization failed: {tokenResponse.Error} - {tokenResponse.ErrorDescription}");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(pollingDelay));
                }
            }
        }
    }
}
