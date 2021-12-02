using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace RabbitMQPlayground.Routing.Shared
{
    public class RabbitMQSubjectExpressionMember
    {
        public string Property { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }
    }

    public class RabbitMQSubjectExpressionVisitor : ExpressionVisitor
    {
        private const string All = "#";
        private const string AllOfSubject = "*";

        private readonly Type _declaringType;
        private readonly List<RabbitMQSubjectExpressionMember> _members = new List<RabbitMQSubjectExpressionMember>();
        private RabbitMQSubjectExpressionMember _current;
        private string _debug;
        private readonly List<string> _usedMembers = new List<string>();

        private readonly IEnumerable<Type> _allowedTypes = new[]
        {
            typeof(sbyte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(byte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(char),
            typeof(string),
            typeof(bool)
        };

        public RabbitMQSubjectExpressionVisitor(Type declaringType)
        {
            _declaringType = declaringType;
        }

        public string Resolve()
        {
            if (_members.Count == 0) return All;

            var expectedRoutingAttributes = _declaringType.GetProperties().Where(property => property.IsDefined(typeof(RoutingPositionAttribute), false))
                                                                          .Count();
            var subject = string.Empty;

            void appendSubject(string str)
            {
                if (string.Empty == subject) subject += str;
                else
                {
                    subject += $".{str}";
                }
            }

            var isStarBounded = false;

            for (var i = 0; i < expectedRoutingAttributes; i++)
            {
                var member = _members.FirstOrDefault(m => m.Position == i);

                if (null== member && !isStarBounded)
                {
                    appendSubject(AllOfSubject);
                    isStarBounded = true;
                } 
                else if ( null != member)
                {
                    appendSubject(member.Value);
                    isStarBounded = false;
                }

            }
                                

            return subject;
        }

        public override Expression Visit(Expression expr)
        {
            if (expr != null)
            {

                switch (expr.NodeType)
                {
                    case ExpressionType.MemberAccess:

                        var memberExpr = (MemberExpression)expr;

                        if (memberExpr.Member.DeclaringType.IsAssignableFrom(_declaringType))
                        {
                            var member = memberExpr.Member.Name;

                            if(!_allowedTypes.Contains(memberExpr.Type) &&  !memberExpr.Type.IsEnum)
                                throw new InvalidOperationException($"property type is not supported [{member}] [{memberExpr.Type}]");

                            if (!memberExpr.Member.CustomAttributes.Any(attr=> attr.AttributeType == typeof(RoutingPositionAttribute)))
                                throw new InvalidOperationException($"only properties decorated with the RoutingPosition attribute are allowed [{member}]");

                            if(_usedMembers.Contains(member))
                                throw new InvalidOperationException($"expression should only referenced member one time [{member}]");

                            _usedMembers.Add(member);

                            var attribute = memberExpr.Member.GetCustomAttributes(typeof(RoutingPositionAttribute), false).First() as RoutingPositionAttribute;

                            _current = new RabbitMQSubjectExpressionMember()
                            {
                                Position = attribute.Position,
                                Property = $"{member}"
                            };

                            _members.Add(_current);

                            _debug += $"{member} ";
                        }

                        break;

                    case ExpressionType.AndAlso:
                        _debug += "AndAlso ";
                        break;

                    case ExpressionType.Equal:
                        _debug += "Equal ";
                        break;

                    case ExpressionType.Constant:

                        var constExp = (ConstantExpression)expr;

                        if (null != _current)
                        {
                            _current.Value = $"{constExp.Value}";
                            _debug += $"{constExp.Value} ";
                        }

                        break;

                    case ExpressionType.Lambda:

                        _members.Clear();
                        _usedMembers.Clear();
                        _current = null;

                        break;
                    case ExpressionType.Parameter:
                        break;

                    //we only handle AndAlso Member Equal type, anything else cannot be rendered as a RabbitMQ subject
                    default:
                        throw new InvalidOperationException($"invalid {expr.NodeType}");

                }

            }

            return base.Visit(expr);
        }

        public override string ToString()
        {
            return _debug;
        }
    }
}
