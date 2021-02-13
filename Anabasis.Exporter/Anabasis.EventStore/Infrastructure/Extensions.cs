using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace Anabasis.EventStore
{
    public static class Extensions
    {
        public static IObservable<TSource> TakeUntilInclusive<TSource>(this IObservable<TSource> source, Func<TSource, Boolean> predicate)
        {
            return Observable.Create<TSource>(observer =>

                source.Subscribe(item =>
                     {
                         observer.OnNext(item);
                         if (predicate(item))
                             observer.OnCompleted();
                     },
                      observer.OnError,
                      observer.OnCompleted
                    )
                  );
        }
    }
}
