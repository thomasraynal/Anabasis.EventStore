using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Contracts
{
    public class SingleDispatcherStrategy : IDispacherStrategy
    {
        public bool CrashAppOnError => true;
    }

    public interface IDispacherStrategy
    {
        bool CrashAppOnError { get; }
    }
}
