﻿using System;
using System.Reflection;

namespace Anabasis.RabbitMQ
{
    public class PropertyToken
    {
        public PropertyToken(int position, Type eventType, PropertyInfo propertyInfo)
        {
            Position = position;
            EventType = eventType;
            PropertyInfo = propertyInfo;
        }

        public int Position { get; }
        public PropertyInfo PropertyInfo { get; }
        public Type EventType { get; }

    }
}
