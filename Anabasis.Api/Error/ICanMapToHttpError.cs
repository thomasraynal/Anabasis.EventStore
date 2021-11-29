using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public interface ICanMapToHttpError
    {
        HttpStatusCode HttpStatusCode { get; }
        string Message { get; }
    }
}
