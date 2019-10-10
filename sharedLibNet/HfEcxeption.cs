using Microsoft.AspNetCore.Http;
using System;

namespace sharedLibNet
{
    public class HfException : Exception
    {
        public HfException()
        {
        }

        public HfException(string message)
            : base(String.Format("HF: {0}", message))
        {
        }

        public HfException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    public class HttpException : HfException
    {
        public HttpException()
        {
        }

        public HttpException(HttpResponse response)
            : base(String.Format("Invalid Http response: statusCode({0}), content: {1}", response.StatusCode, response.Body))
        {
        }

        public HttpException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
