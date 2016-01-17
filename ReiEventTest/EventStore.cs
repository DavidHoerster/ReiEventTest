using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using ReiEventTest.Events;

namespace ReiEventTest
{
    public class EventStore
    {

        public static void LoadDomain(IMongoCollection<ReiEventBase> coll, AggregateRoot root, Guid formId, String rei)
        {
            var events = coll.Find<ReiEventBase>(veb => veb.FormId == formId && veb.ReportingEntityInstanceId == rei)
                             .SortBy(veb => veb.Version);
            root.LoadFromHistory(events.ToEnumerable());
        }

        public static void LoadDomainUpToVersion(IMongoCollection<ReiEventBase> coll, AggregateRoot root, Guid formId, String rei, Int64 version)
        {
            var events = coll.Find<ReiEventBase>(veb => veb.FormId == formId &&
                                            veb.ReportingEntityInstanceId == rei &&
                                            veb.Version <= version)
                 .SortBy(veb => veb.Version);
            root.LoadFromHistory(events.ToEnumerable());
        }

        public static Boolean PersistEvents(IMongoCollection<ReiEventBase> coll, AggregateRoot root)
        {
            var validations = root.GetUncommittedChanges();
            coll.InsertMany(validations.OfType<ReiEventBase>());
            root.MarkChangesAsCommitted();

            return true;
        }

        public static IEnumerable<T> GetControlAnswerHistory<T>(IMongoCollection<ReiEventBase> coll,
                                        Expression<Func<T, Boolean>> filter,
                                        Expression<Func<T, Object>> sortBy) where T : ReiEventBase
        {
            return coll.OfType<T>()
                        .Find<T>(filter)
                        .SortBy(sortBy)
                        .ToEnumerable();
        }

    }
}
