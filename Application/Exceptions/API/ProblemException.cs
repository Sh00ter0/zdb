using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Exceptions.API
{
    
    public class ProblemException: Exception
    {
        public string Error { get; }
        public string Message { get; }
        public int StatusCode { get; } 

        public ProblemException(string error, string message, int statusCode) : base(message)
        {
            Error = error;
            Message = message;
            StatusCode = statusCode;
        }
    }
}
