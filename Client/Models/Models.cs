namespace Client.Models
{
    public class AppApiConfig
    {
        public string headerName { get; set; } = "X-Api-Key";
        public string databasePath { get; set; } = "Data\\api-security.db";
        public string apiKeyHashPepper { get; set; } = "__SET_API_KEY_HASH_PEPPER_VIA_ENV__";
        public bool allowInsecureHttp { get; set; }
        public int rateLimitPermit { get; set; } = 30;
        public int rateLimitWindowSeconds { get; set; } = 60;
        public List<string> knownProxies { get; set; } = [];
        public string masterEncryptionKey { get; set; } = null!;
    }
}
