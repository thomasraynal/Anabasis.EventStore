using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo2
{
    public class Product : BaseAggregate
    {
        public int Quantity { get; set; }
    }
}
