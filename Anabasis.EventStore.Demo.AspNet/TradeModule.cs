using Carter;
using Carter.Request;
using Carter.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class TradeModule : CarterModule
    {
        public TradeModule(TradeSink tradeSink)
        {
            this.Get("/trades", async (ctx) =>
            {
                await ctx.Response.Negotiate(tradeSink.State.GetCurrents());
            });

            this.Get("/trades/{id:long}", async (ctx) =>
            {
                var trade = tradeSink.State.GetCurrents().FirstOrDefault(t => t.EntityId == ctx.Request.RouteValues.As<long>("id"));
                await ctx.Response.Negotiate(trade);
            });
        }
    }
}
