using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.Actor.Exporter
{
  public static class LinqExtensions
  {
    //https://stackoverflow.com/questions/13731796/create-batches-in-linq
    public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size)
    {
      TSource[] bucket = null;
      var count = 0;

      foreach (var item in source)
      {
        if (bucket == null)
          bucket = new TSource[size];

        bucket[count++] = item;
        if (count != size)
          continue;

        yield return bucket;

        bucket = null;
        count = 0;
      }

      if (bucket != null && count > 0)
        yield return bucket.Take(count).ToArray();
    }
  }
}
