using Newtonsoft.Json;

namespace VisualStudio2022.Auth
{
    public class TokenResponse
    {
        //[JsonPropertyName("access_token")]
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        //[JsonPropertyName("id_token")]
        [JsonProperty("id_token")]
        public string IdToken { get; set; }

        //[JsonPropertyName("refresh_token")]
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        //[JsonPropertyName("token_type")]
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        //[JsonPropertyName("expires_in")]
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        //[JsonPropertyName("scope")]
        [JsonProperty("scope")]
        public string Scope { get; set; }

        //[JsonPropertyName("error")]
        [JsonProperty("error")]
        public string Error { get; set; }

        //[JsonPropertyName("error_description")]
        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; }
    }
}
