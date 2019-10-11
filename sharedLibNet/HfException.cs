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

        // todo: docstring
        public HfException(string message)
            : base($"{nameof(HfException)}: {message}")
        {
        }

        // todo: docstring
        public HfException(string message, Exception inner)
            : base($"{nameof(HfException)}: {message}", inner)
        {
        }

        /// <summary>
        /// Http Exception Class that get a HttpResponse Class as argument and make the fault visible by throw a HfException
        /// </summary>
        public HfException(HttpResponseMessage response)
            : base($"Invalid Http response: statusCode({response.StatusCode}), content: {response.Content}")
        {
        }

        /// <summary>
        /// Http Exception Class that get a HttpResponse Class as argument and make the fault visible by throw a HfException
        /// </summary>
        public HfException(HttpResponseMessage response, Exception inner)
            : base($"Invalid Http response: statusCode({response.StatusCode}), content: {inner}")
        {
        }
    }
}
