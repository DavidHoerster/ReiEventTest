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
using System.Diagnostics;
using ReiEventTest.Entity;

namespace ReiEventTest
{
    class Program
    {
        static IMongoCollection<ReiEventBase> _eventCollection;
        static IMongoCollection<Snapshot> _snapshotCollection;
        static IMongoCollection<REI> _reiProjection;

        static void Main(string[] args)
        {
            SetupMongoMaps();

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
                        var history = GetControlAnswerHistory(formId, rei, cmdParts[1]);
                        var controlName = catalog.ControlFields.FirstOrDefault(c => c.Name.Equals(cmdParts[1], StringComparison.OrdinalIgnoreCase)).Name;
                        Console.WriteLine($"History of {controlName}");
                        foreach (var item in history)
                        {
                            Console.WriteLine($"{item.Date} answer of {String.Join(", ", item.Values)}");
                        }
                        break;
                    case "L":
                        rei = cmdParts[1];
                        instance = LoadDomain(formId, rei, catalog);
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
                    case "SN":
                        TakeSnapshot(instance);
                        break;
                    case "LS":
                        instance = LoadFromSnapshot(formId, cmdParts[1]);
                        break;
                    case "P":
                        PersistEvents(instance);
                        break;
                    case "T":
                        Stopwatch sw = new Stopwatch();

                        Console.WriteLine($"Loading REI {cmdParts[1]} with events 100 times is ");
                        sw.Start();
                        for (int i = 0; i < 100; i++)
                        {
                            instance = LoadDomain(formId, cmdParts[1], catalog);
                        }
                        sw.Stop();
                        Console.WriteLine($"{sw.ElapsedMilliseconds} ms");

                        Console.WriteLine($"Loading REI {cmdParts[1]} from snapshot 100 times is ");

                        sw.Reset();
                        sw.Start();
                        for (int i = 0; i < 100; i++)
                        {
                            instance = LoadFromSnapshot(formId, cmdParts[1]);
                        }
                        sw.Stop();

                        Console.WriteLine($"{sw.ElapsedMilliseconds} ms");
                        break;
                    case "PERF":
                        PerfLoadNewRei(formId, cmdParts[1], Int32.Parse(cmdParts[2]), catalog);
                        break;
                    case "V":
                        rei = cmdParts[1];
                        var ver = Int64.Parse(cmdParts[2]);
                        instance = LoadDomainUpToVersion(formId, rei, ver, catalog);
                        Console.WriteLine($"Form ID {formId} REI ID {rei} loaded!!");
                        DisplayDomainStatus(instance);
                        break;
                    case "C":
                        PrintInstructions();
                        break;
                    default:
                        instance.AddAnswer(cmdParts[0], catalog, cmdParts.Skip(1).ToArray());
                        break;
                }
            }
        }

        private static void TakeSnapshot(ICanSnapshot snap)
        {
            var shot = snap.TakeSnapshot();
            EventStore.TakeSnapshot(_snapshotCollection, shot);
        }

        private static ReportingEntityInstance LoadFromSnapshot(Guid formId, String rei)
        {
            var snapshot = EventStore.GetSnapshot(_snapshotCollection, rei);
            var instance = new ReportingEntityInstance(formId, rei);
            instance.LoadSnapshot(snapshot);
            EventStore.LoadDomainStartingAtVersion(_eventCollection, instance, instance.FormDefinitionId, instance.ReportingEntityId, instance.Version);
            return instance;
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

        private static ReportingEntityInstance LoadDomain(Guid formId, string rei, ControlCatalog catalog)
        {
            var instance = new ReportingEntityInstance(formId, rei);
            EventStore.LoadDomain(_eventCollection, instance, formId, rei);
            return instance;
        }

        private static ReportingEntityInstance LoadDomainUpToVersion(Guid formId, String rei, Int64 maxVersion, ControlCatalog catalog)
        {
            var instance = new ReportingEntityInstance(formId, rei);
            EventStore.LoadDomainUpToVersion(_eventCollection, instance, formId, rei, maxVersion);
            return instance;
        }

        private static void PersistEvents(ReportingEntityInstance instance)
        {
            EventStore.PersistEvents(_eventCollection, instance);
            Console.WriteLine("PERSISTED!");

            EventProjector.ProjectReiAnswer(_reiProjection, instance);
            Console.WriteLine("PROJECTED");

            Console.WriteLine("");
        }

        private static IEnumerable<ControlAnswered> GetControlAnswerHistory(Guid formId, String rei, String controlId)
        {
            return EventStore.GetControlAnswerHistory<ControlAnswered>(
                                _eventCollection, 
                                c => c.FormId == formId && c.ReportingEntityInstanceId == rei && c.ControlId == controlId, 
                                c => c.Version);
        }

        private static void PerfLoadNewRei(Guid formId, String rei, Int32 snapInterval, ControlCatalog catalog)
        {
            var newInstance = new ReportingEntityInstance(formId, rei);
            var fields = new String[]{ "FirstName", "LastName", "Age", "Email", "FavoriteFood" };

            Stopwatch sw = new Stopwatch();

            Console.WriteLine($"Starting the creation of {rei}");
            sw.Start();
            for (int i = 0; i < 10000; i++)
            {
                var field = fields[i % 5];
                newInstance.AddAnswer(field, catalog, "answer" + i.ToString());

                EventStore.PersistEvents(_eventCollection, newInstance);

                if (i > 0 && i%snapInterval == 0)
                {
                    var snap = newInstance.TakeSnapshot();
                    EventStore.TakeSnapshot(_snapshotCollection, snap);
                }
            }
            sw.Stop();
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} ms to write 10000 answers with interval of {snapInterval}");

            sw.Reset();

            sw.Start();
            var fullInstance = new ReportingEntityInstance(formId, rei);
            EventStore.LoadDomain(_eventCollection, fullInstance, formId, rei);
            sw.Stop();
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} ms to load domain by replaying events");
            sw.Reset();

            sw.Start();
            var snapInstance = new ReportingEntityInstance(formId, rei);
            snapInstance.LoadSnapshot(EventStore.GetSnapshot(_snapshotCollection, rei));
            EventStore.LoadDomainStartingAtVersion(_eventCollection, snapInstance, formId, rei, snapInstance.Version);
            sw.Stop();
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} ms to load domain from snapshot");

            Console.WriteLine("Replay instance status: ");
            DisplayDomainStatus(fullInstance);

            Console.WriteLine("Snapshot instance status: ");
            DisplayDomainStatus(snapInstance);
        }

        private static void PrintInstructions()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine(" <CTRL_ID> <VAL1> <VAL2> ...");
            Console.WriteLine(" [H]ISTORY <CTRL_ID>");
            Console.WriteLine(" [L]OAD <REI>");
            Console.WriteLine(" [P]ERSIST");
            Console.WriteLine(" TAKE [SN]APSHOT");
            Console.WriteLine(" [LS] LoadSnapshot <REI>");
            Console.WriteLine(" [T]IME TEST Loading All Events and Snapshot for <REI>");
            Console.WriteLine(" [S]TATUS");
            Console.WriteLine(" [F]AILURES");
            Console.WriteLine(" [PERF] <REI> <SNAP_INT> Load up a REI with 10000 events snapshotting every SNAP_INT event");
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
                    new MaxLengthValidator(new Dictionary<String, Object> { {"maxLength", 10 } }),
                    new MinLengthValidator(new Dictionary<string, object> { { "minLength", 1 } }),
                }, "String"),
                new ControlField("LastName", new List<String>(), new List<IControlValidator>
                {
                    new RequiredValidator(),
                    new MaxLengthValidator(new Dictionary<String, Object> { {"maxLength", 40 } }),
                    new MinLengthValidator(new Dictionary<string, object> { { "minLength", 3 } }),
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
                    new MaxLengthValidator(new Dictionary<String, Object> { {"maxLength", 40 } }),
                    new MinLengthValidator(new Dictionary<string, object> { { "minLength", 2 } }),
                }, "String"),
            });

            return catalog;
        }

        private static void SetupMongoMaps()
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

            BsonClassMap.RegisterClassMap<AggregateRoot>(cm =>
            {
                cm.AutoMap();
                cm.AddKnownType(typeof(ReportingEntityInstance));

                cm.SetIsRootClass(true);
            });

            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("Events");
            _eventCollection = db.GetCollection<ReiEventBase>("Validations");
            _snapshotCollection = db.GetCollection<Snapshot>("Snapshots");
            _reiProjection = db.GetCollection<REI>("ReportingEntityInstance");
        }
    }
}