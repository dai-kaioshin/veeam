using System;

namespace GZipTest.Processing
{
    public class ProcessingException : Exception
    {
        public ProcessingException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public ProcessingException(string message) 
            : base(message)
        {
        }
    }
}
