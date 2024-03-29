﻿using Anabasis.RabbitMQ.Event;
using Anabasis.RabbitMQ.Shared;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
   public class TestSubjectSerialization
    {

        class TestEvent : BaseRabbitMqEvent
        {
            public TestEvent(Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
            {
            }

            public object Invalid { get; set; }

            [RoutingPosition(0)]
            public string AggregateId { get; set; }

            public string Broker { get; set; }

            [RoutingPosition(1)]
            public string Market { get; set; }

            [RoutingPosition(2)]
            public string Counterparty { get; set; }

            [RoutingPosition(3)]
            public string Exchange { get; set; }

        }

        [Test]
        public void ShouldNotSerializeAnEventAsRabbitSubject()
        {

            //non routable property
            Expression<Func<TestEvent, bool>> validExpression = (ev) => (ev.AggregateId == "MySmallBusiness" && (ev.Broker == "Newedge" && ev.Counterparty == "SGCIB") && ev.Exchange == "SmallCap");

            var visitor = new TopicExchangeRabbitMQSubjectResolver(typeof(TestEvent));

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });


            //type not allowed
            validExpression = (ev) => ev.Invalid == null;

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });


            //member multiple reference
            validExpression = (ev) => ev.AggregateId == "EUR/USD" && ev.AggregateId == "EUR/GBP";

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });

            //too complex lambda 
            validExpression = (ev) => ev.AggregateId == "EUR/USD" && ev.Market != "Euronext";

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });

        }

        [Test]
        public void ShouldSerializeAnEventAsRabbitSubject()
        {

            Expression<Func<TestEvent, bool>> validExpression = (ev) => (ev.AggregateId == "MySmallBusiness" && (ev.Market == "Euronext" && ev.Counterparty == "SGCIB") && ev.Exchange == "SmallCap");

            var visitor = new TopicExchangeRabbitMQSubjectResolver(typeof(TestEvent));

            visitor.Visit(validExpression);

            var subject = visitor.GetSubject();

            Assert.AreEqual("TestEvent.MySmallBusiness.Euronext.SGCIB.SmallCap", subject);

            validExpression = (ev) => true;

            visitor.Visit(validExpression);

            subject = visitor.GetSubject();

            Assert.AreEqual("TestEvent.#", subject);

            validExpression = (ev) => ev.AggregateId == "MySmallBusiness";

            visitor.Visit(validExpression);

            subject = visitor.GetSubject();

            Assert.AreEqual("TestEvent.MySmallBusiness.*.*.*", subject);

            validExpression = (ev) => ev.Market == "Euronext";

            visitor.Visit(validExpression);

            subject = visitor.GetSubject();

            Assert.AreEqual("TestEvent.*.Euronext.*.*", subject);

        }
    }
}
