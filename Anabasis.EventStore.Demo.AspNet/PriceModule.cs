using Carter;
using Carter.Request;
using Carter.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class PriceModule : CarterModule
    {
        public PriceModule(MarketDataSink marketDataSink)
        {
            this.Get("/ccy", async (ctx) =>
            {
                await ctx.Response.Negotiate(marketDataSink.State.GetCurrents());
            });

            this.Get("/ccy/{id}", async (ctx) =>
            {
                var trade = marketDataSink.State.GetCurrents().FirstOrDefault(t => t.EntityId == ctx.Request.RouteValues.As<string>("id"));
                await ctx.Response.Negotiate(trade);
            });
        }
    }
}
