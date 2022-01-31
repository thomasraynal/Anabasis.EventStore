using NUnit.Framework;
using System;

namespace Anabasis.Api.Tests.Common
{
    public static class TestHelper
    {
        public static bool IsAppVeyor => Environment.GetEnvironmentVariable("APPVEYOR") != null;

    }
}
