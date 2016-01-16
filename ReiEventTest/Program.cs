using Cti.Platform.Events;
using Cti.RegulatoryReporting.Entity.Form.Controls;
using Cti.RegulatoryReporting.Entity.Form.ControlValidation;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ReiEventTest.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var coll = SetupMongoMaps();

            var formId = new Guid("cca74fb0-87ae-4e42-8d48-7865de6f130c");
            ReportingEntityInstance instance = null;
            String rei = null;
            var catalog = SetUpFormControls(formId);

            PrintInstructions();

            while (true)
            {
                var cmd = Console.ReadLine();
                var cmdParts = cmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                switch (cmdParts[0])
                {
                    case "Q":
                        Console.WriteLine("BYE!!");
                        return;
                        break;
                    case "H":
                        var history = GetControlAnswerHistory(coll, formId, rei, cmdParts[1]);
                        var controlName = catalog.ControlFields.FirstOrDefault(c => c.Name.Equals(cmdParts[1], StringComparison.OrdinalIgnoreCase)).Name;
                        Console.WriteLine($"History of {controlName}");
                        foreach (var item in history)
                        {
                            Console.WriteLine($"{item.Date} answer of {String.Join(", ", item.Values)}");
                        }
                        break;
                    case "L":
                        rei = cmdParts[1];
                        instance = LoadDomain(coll, formId, rei, catalog);
                        Console.WriteLine($"Form ID {formId} REI ID {rei} loaded!!");
                        break;
                    case "F":
                        var failures = instance.GetFailingControls();
                        Console.WriteLine("Current failing controls...");
                        foreach (var fail in failures)
                        {
                            Console.WriteLine($"{fail.ControlId} {fail.State} {fail.Validator} {fail.Message}");
                        }
                        Console.WriteLine("");
                        break;

                    case "S":
                        DisplayDomainStatus(instance);
                        break;
                    case "P":
                        PersistEvents(coll, instance);

                        Console.WriteLine("PERSISTED!");
                        Console.WriteLine("");
                        break;
                    case "V":
                        rei = cmdParts[1];
                        var ver = Int64.Parse(cmdParts[2]);
                        instance = LoadDomainUpToVersion(coll, formId, rei, ver, catalog);
                        Console.WriteLine($"Form ID {formId} REI ID {rei} loaded!!");
                        DisplayDomainStatus(instance);
                        break;
                    case "C":
                        PrintInstructions();
                        break;
                    default:
                        instance.AddAnswer(cmdParts[0], cmdParts.Skip(1).ToArray());
                        break;
                }
            }
        }

        private static void DisplayDomainStatus(ReportingEntityInstance instance)
        {
            Console.WriteLine("Current validation status for all controls...");

            var status = instance.GetStatus();
            foreach (var stat in status)
            {
                Console.WriteLine($"{stat.ControlId} {stat.State} {stat.Validator} {stat.Message}");
            }
            Console.WriteLine("");

            Console.WriteLine("Current answers for all controls...");

            var answers = instance.GetAnswers();
            foreach (var answer in answers)
            {
                Console.WriteLine($"{answer.ControlId} has value(s) {String.Join(", ", answer.Values)} with timestamp {answer.Timestamp}");
            }
            Console.WriteLine("");
        }

        private static ReportingEntityInstance LoadDomain(IMongoCollection<ReiEventBase> coll, Guid formId, string rei, ControlCatalog catalog)
        {
            var instance = new ReportingEntityInstance(formId, rei, catalog);
            var events = coll.Find<ReiEventBase>(veb => veb.FormId == formId && veb.ReportingEntityInstanceId == rei)
                             .SortBy(veb => veb.Version);
            instance.LoadFromHistory(events.ToEnumerable());
            return instance;
        }

        private static ReportingEntityInstance LoadDomainUpToVersion(IMongoCollection<ReiEventBase> coll, Guid formId, String rei, Int64 maxVersion, ControlCatalog catalog)
        {
            var instance = new ReportingEntityInstance(formId, rei, catalog);
            var events = coll.Find<ReiEventBase>(veb => veb.FormId == formId && 
                                                        veb.ReportingEntityInstanceId == rei &&
                                                        veb.Version <= maxVersion)
                             .SortBy(veb => veb.Version);
            instance.LoadFromHistory(events.ToEnumerable());
            return instance;
        }

        private static void PersistEvents(IMongoCollection<ReiEventBase> coll, ReportingEntityInstance instance)
        {
            var validations = instance.GetUncommittedChanges();
            coll.InsertMany(validations.OfType<ReiEventBase>());
            instance.MarkChangesAsCommitted();
        }

        private static IEnumerable<ControlAnswered> GetControlAnswerHistory(IMongoCollection<ReiEventBase> coll, Guid formId, String rei, String controlId)
        {
            return coll.OfType<ControlAnswered>()
                                .Find<ControlAnswered>(c => c.FormId == formId && c.ReportingEntityInstanceId == rei && c.ControlId == controlId)
                                .SortBy(c => c.Version)
                                .ToEnumerable();
        }

        private static void PrintInstructions()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine(" <CTRL_ID> <VAL1> <VAL2> ...");
            Console.WriteLine(" [H]ISTORY <CTRL_ID>");
            Console.WriteLine(" [L]OAD <REI>");
            Console.WriteLine(" [P]ERSIST");
            Console.WriteLine(" [S]TATUS");
            Console.WriteLine(" [F]AILURES");
            Console.WriteLine(" [V]ERSION <VERSION>");
            Console.WriteLine(" [C]OMMANDS");
            Console.WriteLine(" [Q]UIT");

            Console.WriteLine("");
        }

        private static ControlCatalog SetUpFormControls(Guid formId)
        {
            var catalog = new ControlCatalog(Guid.NewGuid(), formId, new List<ControlField>
            {
                new ControlField("FirstName", new List<String>(), new List<IControlValidator>
                {
                    new RequiredValidator(),
                    new MaxLengthValidator(new Dictionary<String, Object> { {"maxLength", 10 } })
                }, "String"),
                new ControlField("LastName", new List<String>(), new List<IControlValidator>
                {
                    new RequiredValidator(),
                    new MaxLengthValidator(new Dictionary<String, Object> { {"maxLength", 40 } })
                }, "String"),
                new ControlField("Age", new List<String>(), new List<IControlValidator>
                {
                    new RequiredValidator(),
                    new NumericValidator()
                }, "Number"),
                new ControlField("Email", new List<String>(), new List<IControlValidator>
                {
                    new RegexValidator(new Dictionary<String, Object> { { "message", "BOO!" }, { "pattern", @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$" } })
                }, "String"),
                new ControlField("FavoriteFood", new List<String>(), new List<IControlValidator>
                {
                    new MaxLengthValidator(new Dictionary<String, Object> { {"maxLength", 40 } })
                }, "String"),
            });

            return catalog;
        }

        private static IMongoCollection<ReiEventBase> SetupMongoMaps()
        {
            BsonClassMap.RegisterClassMap<EventBase>(cm =>
            {
                cm.AutoMap();
                cm.AddKnownType(typeof(ReiEventBase));
                cm.AddKnownType(typeof(ControlFieldEventBase));
                cm.AddKnownType(typeof(ControlAnswered));
                cm.AddKnownType(typeof(ValidationEventBase));
                cm.AddKnownType(typeof(ValidationPassed));
                cm.AddKnownType(typeof(ValidationFailed));

                cm.SetIsRootClass(true);
            });

            var client = new MongoClient("mongodb://localhost:27020");
            var db = client.GetDatabase("Events");
            var coll = db.GetCollection<ReiEventBase>("Validations");

            return coll;
        }
    }
}