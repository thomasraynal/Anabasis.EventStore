using Anabasis.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public interface IWorkerConfiguration
    {
        int DispatcherCount { get; set; }
        bool SwallowUnknownEvent { get; set; }
        IDispacherStrategy DispacherStrategy { get; set; }
        IMessageGroupStrategy MessageGroupStrategy { get; set; }
    }
}
