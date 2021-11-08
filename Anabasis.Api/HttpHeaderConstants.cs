using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public static class HttpHeaderConstants
    {
        public const string HTTP_HEADER_CORRELATION_ID = "x-correlation-id";
        public const string HTTP_HEADER_REQUEST_ID = "x-request-id";
        public const string HTTP_HEADER_APP_NAME = "x-application-name";
        public const string HTTP_HEADER_API_VERSION = "x-api-version";
        public const string HTTP_HEADER_OPERATION_NAME = "x-operation-name";
        public const string HTTP_HEADER_CLIENT_IP_ADRESSS = "x-client-ip-address";
    }
}
