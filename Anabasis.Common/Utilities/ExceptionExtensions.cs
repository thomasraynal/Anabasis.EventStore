using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Utilities
{
    public static class ExceptionExtensions
    {
        public static Exception GetActualException(this Exception exception)
        {
      
            if (exception is AggregateException aggregateException)
            {
                return aggregateException.Flatten().InnerException ?? exception;
            }
            else
            {
                return exception;
            }
        }
    }
}
