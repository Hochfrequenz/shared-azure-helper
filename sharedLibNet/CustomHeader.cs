namespace sharedLibNet
{
    /// <summary>
    /// This class contains string constants with HTTP header keys. Having them hard coded here and only here allows for easy modifications.
    /// </summary>
    public abstract class CustomHeader
    {
        public static readonly string XArrClientCert = "X-ARR-ClientCert";
        public static readonly string OcpApimSubscriptionKey = "Ocp-Apim-Subscription-Key";
        public static readonly string HfClientCert = "HF-ClientCert";
        public static readonly string HfAuthorization = "HF-Authorization";
        public static readonly string Authorization = "Authorization";
    }
}
