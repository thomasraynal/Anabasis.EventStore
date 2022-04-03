using DynamicData;
using System;
using System.Reactive;

namespace Anabasis.Common.Utilities
{
    public static class DynamicDataExtensions
    {
        public static IObservable<IChangeSet<TObject, TKey>> FilterEvents<TObject, TKey>(this IObservable<IChangeSet<TObject, TKey>> source, Func<TObject, bool> filter)
            where TKey : notnull
        {
            return source.Filter(filter).WhereReasonsAreNot(ChangeReason.Remove);
        }

        public static IObservable<IChangeSet<TObject, TKey>> FilterEvents<TObject, TKey>(this IObservable<IChangeSet<TObject, TKey>> source, IObservable<Func<TObject, bool>> predicateChanged)
            where TKey : notnull
        {
            return source.Filter(predicateChanged).WhereReasonsAreNot(ChangeReason.Remove);
        }

        public static IObservable<IChangeSet<TObject, TKey>> FilterEvents<TObject, TKey>(this IObservable<IChangeSet<TObject, TKey>> source, IObservable<Unit> reapplyFilter)
            where TKey: notnull
        {
            return source.Filter(reapplyFilter).WhereReasonsAreNot(ChangeReason.Remove);
        }

        public static IObservable<IChangeSet<TObject, TKey>> FilterEvents<TObject, TKey>(this IObservable<IChangeSet<TObject, TKey>> source, IObservable<Func<TObject, bool>> predicateChanged, IObservable<Unit> reapplyFilter)
            where TKey : notnull
        {
            return source.Filter(predicateChanged, reapplyFilter).WhereReasonsAreNot(ChangeReason.Remove);
        }

    }
}
