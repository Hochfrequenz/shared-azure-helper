using System;
using System.Collections.Generic;
using System.Text;

namespace sharedLibNet
{
    public abstract class CustomHeader
    {
        public static readonly string XArrClientCert = "X-ARR-ClientCert";
        public static readonly string OcpApimSubscriptionKey = "Ocp-Apim-Subscription-Key";
        public static readonly string HfClientCert = "HF-ClientCert";
        public static readonly string HfAuthorization = "HF-Authorization";
        public static readonly string Authorization = "Authorization";
    }
}
