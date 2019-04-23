namespace sharedLibNet
{
    /// <summary>
    /// This class contains string constants with HTTP header keys. Having them hard coded here and only here allows for easy modifications.
    /// </summary>
    public abstract class CustomHeader
    {
        public const string XArrClientCert = "X-ARR-ClientCert";
        public const string OcpApimSubscriptionKey = "Ocp-Apim-Subscription-Key";
        public const string HfClientCert = "HF-ClientCert";
        public const string HfAuthorization = "HF-Authorization";
        public const string Authorization = "Authorization";
        public const string BackendId = "BOBackendId";
    }
}
