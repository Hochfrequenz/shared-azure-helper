using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;

namespace sharedLibNet
{
    /// <summary>
    /// Main HF CustomException Class 
    /// </summary>
    public class HfException : Exception
    {
        public HfException()
        {
        }

        public HfException(string message)
            : base(String.Format("HF exception: {0}", message))
        {
        }

        public HfException(string message, Exception inner)
            : base(String.Format("HF exception: {0}", message), inner)
        {
        }

        /// <summary>
        /// Http Exception Class that get a HttpResponse Class as argument and make the fault visible by throw a HfException
        /// </summary>
        public HfException(HttpResponseMessage response)
            : base(String.Format("Invalid Http response: statusCode({0}), content: {1}", response.StatusCode, response.Content))
        {
        }

        /// <summary>
        /// Http Exception Class that get a HttpResponse Class as argument and make the fault visible by throw a HfException
        /// </summary>
        public HfException(HttpResponseMessage response, Exception inner)
            : base(String.Format("Invalid Http response: statusCode({0}), content: {1}", response.StatusCode, response.Content), inner)
        {
        }
    }
}
