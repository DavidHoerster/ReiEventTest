using Cti.RegulatoryReporting.Entity.Form.Controls;
using Cti.RegulatoryReporting.Entity.Form.ControlValidation;
using ReiEventTest.Events;
using ReiEventTest.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest
{
    public class ReportingEntityInstance : AggregateRoot, ICanSnapshot
    {
        public ReportingEntityInstance(Guid formId, String reportingid)
        {
            FormDefinitionId = formId;
            ReportingEntityId = reportingid;
            ControlStatus = new Dictionary<String, ControlValidatorStatus>();
            ControlAnswers = new Dictionary<String, ControlAnswer>();
        }

        public IDictionary<String, ControlValidatorStatus> ControlStatus { get; private set; }
        public IDictionary<String, ControlAnswer> ControlAnswers { get; private set; }
        public String ReportingEntityId { get; private set; }
        public Guid FormDefinitionId { get; private set; }

        public void AddAnswer(String controlId, ControlCatalog catalog, IEnumerable<ControlRule> rules, params Object[] answers)
        {
            var date = DateTime.UtcNow;
            ApplyChange(new ControlAnswered
            {
                ControlId = controlId,
                Date = date,
                FormId = FormDefinitionId,
                Id = Guid.NewGuid(),
                ReportingEntityInstanceId = ReportingEntityId,
                Version = Version,
                Values = answers,
            });

            var msgs = ValidateAnswers(controlId, catalog, answers);
            foreach (var msg in msgs)
            {
                if (msg.IsValid)
                {
                    ApplyChange(new ValidationPassed
                    {
                        ControlId = controlId,
                        Date = date,
                        FormId = FormDefinitionId,
                        Id = Guid.NewGuid(),
                        ReportingEntityInstanceId = ReportingEntityId,
                        Message = msg.Message.Message,
                        Version = Version,
                        Validator = msg.Message.ObjectName,
                        Values = answers,
                    });
                }
                else
                {
                    ApplyChange(new ValidationFailed
                    {
                        ControlId = controlId,
                        Date = date,
                        FormId = FormDefinitionId,
                        Id = Guid.NewGuid(),
                        ReportingEntityInstanceId = ReportingEntityId,
                        Message = msg.Message.Message,
                        Version = Version,
                        Validator = msg.Message.ObjectName,
                        Values = answers,
                    });
                }
            }

            RunRules(controlId, catalog, rules, date, answers);

        }

        public IEnumerable<ControlValidatorStatus> GetFailingControls()
        {
            return ControlStatus.Where(c => c.Value.State == false)
                                    .Select(c => c.Value);
        }

        public IEnumerable<ControlAnswer> GetAnswers()
        {
            return ControlAnswers.Select(a => a.Value);
        }

        public IEnumerable<ControlValidatorStatus> GetStatus()
        {
            return ControlStatus.Select(c => c.Value);
        }

        private void RunRules(String controlId, ControlCatalog catalog, IEnumerable<ControlRule> rules, DateTime now, Object[] answers)
        {
            var results = new List<RuleEvaluated>();
            var validRules = rules.Where(r => r.ControlId == controlId);
            foreach (var rule in validRules)
            {
                var calc = new NCalc.Expression(rule.Rule.If);
                foreach (var answer in answers)
                {
                    calc.Parameters.Clear();
                    calc.Parameters.Add(controlId, answer);
                    var result = (Boolean)calc.Evaluate();

                    ApplyChange(new RuleEvaluated
                    {
                        ControlId = controlId,
                        Date = now,
                        FormId = rule.FormId,
                        Id = Guid.NewGuid(),
                        ReportingEntityInstanceId = ReportingEntityId,
                        Result = result ? rule.Rule.Then : rule.Rule.Else,
                        RuleName = rule.Rule.Name,
                        Values = answers,
                        Version = Version
                    });
                }
            }
        }

        private IEnumerable<ValidationMessage> ValidateAnswers(String controlId, ControlCatalog catalog, Object[] answers)
        {
            var msgs = new List<ValidationMessage>();
            var control = catalog.ControlFields.FirstOrDefault(c => c.Name.Equals(controlId, StringComparison.OrdinalIgnoreCase));
            if (control == null)
            {
                //not found
                return msgs;
            }
            foreach (var val in control.ControlValidations)
            {
                foreach (var answer in answers)
                {
                    var msg = val.Validate(answer);
                    msg.Message.ObjectName = val.GetType().Name;
                    msgs.Add(msg);
                }
            }

            return msgs;
        }

        private void Apply(ValidationFailed evt)
        {
            var key = $"{evt.ControlId}-{evt.Validator}";
            if (ControlStatus.ContainsKey(key))
            {
                var item = ControlStatus[key];
                item.Values = evt.Values;
                item.State = false;
                item.Message = evt.Message;
                item.Timestamp = evt.Version;
            }
            else
            {
                var item = new ControlValidatorStatus
                {
                    ControlId = evt.ControlId,
                    Message = evt.Message,
                    State = false,
                    Timestamp = evt.Version,
                    Values = evt.Values,
                    Validator = evt.Validator
                };
                ControlStatus.Add(key, item);
            }
            Version = evt.Version;
        }

        private void Apply(ValidationPassed evt)
        {
            var key = $"{evt.ControlId}-{evt.Validator}";
            if (ControlStatus.ContainsKey(key))
            {
                var item = ControlStatus[key];
                item.Values = evt.Values;
                item.State = true;
                item.Message = evt.Message;
                item.Timestamp = evt.Version;
            }
            else
            {
                var item = new ControlValidatorStatus
                {
                    ControlId = evt.ControlId,
                    Message = evt.Message,
                    State = true,
                    Timestamp = evt.Version,
                    Values = evt.Values,
                    Validator = evt.Validator
                };
                ControlStatus.Add(key, item);
            }
            Version = evt.Version;
        }

        private void Apply(ControlAnswered evt)
        {
            var key = evt.ControlId;
            if (ControlAnswers.ContainsKey(key))
            {
                var item = ControlAnswers[key];
                item.Values = evt.Values;
                item.Date = evt.Date;
                item.Timestamp = evt.Version;
            }
            else
            {
                var item = new ControlAnswer
                {
                    ControlId = evt.ControlId,
                    Date = evt.Date,
                    Timestamp = evt.Version,
                    Values = evt.Values
                };
                ControlAnswers.Add(key, item);
            }
            Version = evt.Version;
        }

        private void Apply(RuleEvaluated evt)
        {
            //probably need to drop this on a bus
            Console.WriteLine($"{evt.RuleName} for {evt.ControlId} in REI {evt.ReportingEntityInstanceId} evaluated to {evt.Result}");
        }

        public Snapshot TakeSnapshot()
        {
            return new Snapshot(Guid.NewGuid(), ReportingEntityId, Version, new Dictionary<String, Object>
            {
                { "answers", ControlAnswers },
                { "status", ControlStatus }
            });
        }

        public void LoadSnapshot(Snapshot snap)
        {
            Version = snap.Version;
            ControlAnswers = snap.State["answers"] as Dictionary<String, ControlAnswer>;
            ControlStatus = snap.State["status"] as Dictionary<String, ControlValidatorStatus>;
        }
    }
}
