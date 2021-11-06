using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public static class WebConstants
    {
        public const string CORRELATION_ID_HTTP_HEADER = "x-correlationid";
        public const string REQUEST_ID_HTTP_HEADER = "x-requestid";
        public const string APP_NAME_HTTP_HEADER = "x-application-name";
        public const string API_VERSION = "x-api-version";
        public const string OPERATION_NAME = "X-operation-name";
    }
}
