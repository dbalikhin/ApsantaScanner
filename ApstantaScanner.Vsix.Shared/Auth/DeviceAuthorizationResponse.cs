using Newtonsoft.Json;

namespace ApstantaScanner.Vsix.Shared.Auth
{
    public class DeviceAuthorizationResponse
    {
        //[JsonPropertyName("device_code")]
        [JsonProperty("device_code")]
        public string DeviceCode { get; set; }

        //[JsonPropertyName("user_code")]
        [JsonProperty("user_code")]
        public string UserCode { get; set; }

        //[JsonPropertyName("verification_uri")]
        [JsonProperty("verification_uri")]
        public string VerificationUri { get; set; }

        //[JsonPropertyName("expires_in")]
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        //[JsonPropertyName("interval")]
        [JsonProperty("interval")]
        public int Interval { get; set; }
    }
}
