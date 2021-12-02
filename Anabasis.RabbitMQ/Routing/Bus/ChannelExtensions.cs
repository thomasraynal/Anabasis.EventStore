using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ.Routing.Bus
{
    public static class ChannelExtensions
    {

        public static bool DoesExchangeExist(this IModel model, string exchangeName)
        {
            try
            {
                model.ExchangeDeclarePassive(exchangeName);
                return true;
            }
            //todo: catch the proper exception
            catch (Exception)
            {
                return false;
            }
        }

    }
}
