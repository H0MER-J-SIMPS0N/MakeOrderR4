using Newtonsoft.Json;

namespace MakeOrderR4v2.Models
{
    public class TokenResponse
    {
        #region Fields and Properties
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        #endregion
    }
}
