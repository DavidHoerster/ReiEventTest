using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Rules
{
    public enum Outcome
    {
        Enable = 0,
        Disable,
        Validation
    }
    public abstract class RuleOutcome
    {
        public String ControlId { get; set; }
        public String Message { get; set; }
    }

    public class NoActionOutcome : RuleOutcome
    {
    }

    public class EnableOutcome : RuleOutcome
    {
    }

    public class DisableOutcome : RuleOutcome
    {
    }

    public class FailedValidationOutcome : RuleOutcome
    {
        public String ValidatorName { get; set; }
    }
    public class PassedValidationOutcome : RuleOutcome
    {
        public String ValidatorName { get; set; }
    }

}
