
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

        /// <summary>
        /// Exception Constructor that gets a string as argument and makes the Error visible
        /// </summary>
        /// <param name="message">Message of exception</param>
        public HfException(string message)
            : base($"{nameof(HfException)}: {message}")
        {
        }

        /// <summary>
        /// Exception Constructor that gets a string as argument and makes the Error visible.
        /// </summary>
        /// <param name="message">Message of current exception.</param>
        /// <param name="inner">InnerException property returns the original exception that caused the current exception.</param>
        public HfException(string message, Exception inner)
            : base($"{nameof(HfException)}: {message}", inner)
        {
        }

        /// <summary>
        /// Exception Constructor that gets a Exception Class as argument and makes the Error visible.
        /// </summary>
        /// <param name="e">Exception object</param>
        public HfException(Exception e)
            : base($"{nameof(HfException)}: {e.Message}")
        {
        }
        /// <summary>
        /// Exception Constructor that gets a Exception Class as argument and makes the Error visible.
        /// </summary>
        /// <param name="message">Message of current exception.</param>
        /// <param name="inner">InnerException property returns the original exception that caused the current exception.</param>
        public HfException(Exception e, Exception inner)
            : base($"{nameof(HfException)}: {e.Message}", inner)
        {
        }

        /// <summary>
        /// Http Exception Constructor that gets a HttpResponseMessage Class as argument and makes the Error visible.
        /// </summary>
        /// <param name="response">HttpResponseMessage object that caused this excetion.</param>

        public HfException(HttpResponseMessage response)
            : base($"Invalid Http response: statusCode({response.StatusCode}), content: {response.Content}")
        {
        }

        /// <summary>
        /// Http Exception Constructor that gets a HttpResponseMessage Class as argument and makes the Error visible.
        /// </summary>
        /// <param name="response">HttpResponseMessage object that caused this excetion.</param>
        /// <param name="inner">InnerException property returns the original exception that caused the current exception.</param>
        public HfException(HttpResponseMessage response, Exception inner)
            : base($"Invalid Http response: statusCode({response.StatusCode}), content: {inner}")
        {
        }
    }
}
