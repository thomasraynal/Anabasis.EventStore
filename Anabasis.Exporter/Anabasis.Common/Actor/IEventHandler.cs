using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Actor
{
  public interface IEventHandler
  {
    Task Handle(Message message);
  }
}
