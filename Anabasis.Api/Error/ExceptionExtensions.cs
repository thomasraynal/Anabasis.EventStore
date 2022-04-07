using System;

namespace Anabasis.Api
{
    public static class ExceptionData
    {
        public const string CalledUrl = "calledUrl";
        public const string CorrelationId = "correlationId";
        public const string HttpMethod = "httpMethod";
        public const string RequestId = "requestId";
        public const string MachineName = "machineName";
    }

    public static class ExceptionExtensions
    {

#nullable disable

        public static void SetData<TData>(this Exception ex, string key, TData value) => ex.Data[key] = $"{value}";
        public static string GetData(this Exception ex, string key) => ex.Data[key] as string;

#nullable enable

    }


}
