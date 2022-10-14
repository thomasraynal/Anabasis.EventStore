using Anabasis.RabbitMQ.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo2
{
    public class ProductInventoryChanged : BaseRabbitMqEvent
    {
        public ProductInventoryChanged(Guid eventID, Guid correlationId) : base(null, eventID, correlationId)
        {

        }

        public string ProductId { get; set; }
        public int CurrentInventory { get; set; }
    }
}
