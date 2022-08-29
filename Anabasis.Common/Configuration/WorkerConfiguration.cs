using Anabasis.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{


    public class WorkerConfiguration : IWorkerConfiguration
    {
        public int DispatcherCount { get; set; } = 1; 

        public bool SwallowUnknownEvent { get; set; } = true;

        public IDispacherStrategy DispacherStrategy { get; set; } = new SingleDispatcherStrategy();

        public IMessageGroupStrategy MessageGroupStrategy { get; set; } = new NoMessageGroupStrategy();
    }
}
