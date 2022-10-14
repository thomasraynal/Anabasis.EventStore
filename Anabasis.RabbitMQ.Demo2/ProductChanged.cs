using Anabasis.Common;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Anabasis.RabbitMQ.Demo2
{
    public class ProductChanged : BaseAggregateEvent<Product>
    {
        public ProductChanged(string productName, Guid? correlationId = null, Guid? causeId = null) : base(productName, correlationId, causeId)
        {
        }

        public int CurrentQuantity { get; set; }

        public override void Apply([NotNull] Product entity)
        {
            entity.Quantity = CurrentQuantity;
        }
    }
}
