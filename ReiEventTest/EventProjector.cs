using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReiEventTest.Entity;

namespace ReiEventTest
{
    public class EventProjector
    {

        public static void ProjectReiAnswer(IMongoCollection<REI> coll, ReportingEntityInstance rei)
        {
            var entity = new REI(rei.ReportingEntityId, rei.FormDefinitionId,
                rei.ControlAnswers.Select(a => new Answer(a.Value.ControlId, a.Value.Values)));
            if(coll.AsQueryable().Any(r => r.Id == rei.ReportingEntityId))
            {
                //update
                coll.ReplaceOne(r => r.Id == entity.Id, entity);
            }
            else
            {
                coll.InsertOne(entity);
            }
        }
    }
}