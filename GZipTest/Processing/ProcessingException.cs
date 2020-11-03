using System;

namespace GZipTest.Processing
{
    class ProcessingException : Exception
    {
        internal ProcessingException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        internal ProcessingException(string message) 
            : base(message)
        {
        }
    }
}
