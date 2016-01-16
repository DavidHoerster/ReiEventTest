using Cti.RegulatoryReporting.Entity.Form.Controls;
using Cti.RegulatoryReporting.Entity.Form.ControlValidation;
using ReiEventTest.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest
{
    public class ReportingEntityInstance : AggregateRoot
    {
        public readonly String ReportingEntityId;
        public readonly Guid FormDefinitionId;
        private IDictionary<String, ControlValidatorStatus> _controlStatus;
        private IDictionary<String, ControlAnswer> _controlAnswers;
        private readonly ControlCatalog Catalog;

        public ReportingEntityInstance(Guid formId, String reportingid, ControlCatalog controlCatalog)
        {
            FormDefinitionId = formId;
            ReportingEntityId = reportingid;
            Catalog = controlCatalog;
            _controlStatus = new Dictionary<String, ControlValidatorStatus>();
            _controlAnswers = new Dictionary<String, ControlAnswer>();

            //Version = DateTime.UtcNow.Ticks;
        }

        public void AddAnswer(String controlId, params Object[] answers)
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

            var msgs = ValidateAnswers(controlId, answers);
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
        }

        public IEnumerable<ControlValidatorStatus> GetFailingControls()
        {
            return _controlStatus.Where(c => c.Value.State == false)
                                    .Select(c => c.Value);
        }

        public IEnumerable<ControlAnswer> GetAnswers()
        {
            return _controlAnswers.Select(a => a.Value);
        }

        public IEnumerable<ControlValidatorStatus> GetStatus()
        {
            return _controlStatus.Select(c => c.Value);
        }

        private IEnumerable<ValidationMessage> ValidateAnswers(String controlId, Object[] answers)
        {
            var msgs = new List<ValidationMessage>();
            var control = Catalog.ControlFields.FirstOrDefault(c => c.Name.Equals(controlId, StringComparison.OrdinalIgnoreCase));
            if (control == null)
            {
                //not found
                return msgs;
            }
            foreach (var val in control.ControlValidations)
            {
                foreach (var answer in answers)
                {
                    var msg = val.Validate(Catalog.Id, answer);
                    msg.Message.ObjectName = val.GetType().Name;
                    msgs.Add(msg);
                }
            }

            return msgs;
        }

        private void Apply(ValidationFailed evt)
        {
            var key = $"{evt.ControlId}-{evt.Validator}";
            if (_controlStatus.ContainsKey(key))
            {
                var item = _controlStatus[key];
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
                _controlStatus.Add(key, item);
            }
            Version = evt.Version;
        }

        private void Apply(ValidationPassed evt)
        {
            var key = $"{evt.ControlId}-{evt.Validator}";
            if (_controlStatus.ContainsKey(key))
            {
                var item = _controlStatus[key];
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
                _controlStatus.Add(key, item);
            }
            Version = evt.Version;
        }

        private void Apply(ControlAnswered evt)
        {
            var key = evt.ControlId;
            if (_controlAnswers.ContainsKey(key))
            {
                var item = _controlAnswers[key];
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
                _controlAnswers.Add(key, item);
            }
            Version = evt.Version;
        }
    }
}
