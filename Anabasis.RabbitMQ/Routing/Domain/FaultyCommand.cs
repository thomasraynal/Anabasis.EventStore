using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class FaultyCommand : CommandBase
    {
        public FaultyCommand(string aggregateId, string target) : base(aggregateId, target)
        {
        }

        public string Property
        {
            get
            {
                return "Property";
            }

            set
            {
                throw new Exception("boom");
            }
        }
    }
}
