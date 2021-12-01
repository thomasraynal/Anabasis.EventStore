using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqTransientErrorDetectionStrategy
    {
        //public bool IsTransient(Exception ex)
        //{
        //    ex = ex.GetTheGoodException();

        //    if (ex is OperationInterruptedException)
        //        return true;
        //    if (ex is SocketException)
        //        return true;
        //    if (ex is NotSupportedException)
        //        return true;
        //    if (ex is IOException)
        //        return true;
        //    if (ex is BeezUPRabbitMqException)
        //        return true;
        //    if (ex is TimeoutException)
        //        return true;
        //    if (ex is AlreadyClosedException)
        //        return true;

        //    return false;
        //}
    }
}
