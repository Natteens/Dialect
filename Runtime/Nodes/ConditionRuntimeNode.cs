using System;
using Dialect.Conditions;
using Dialect.Core;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class ConditionRuntimeNode : RuntimeNode
    {
        [SerializeField] DialectCondition condition;
        [SerializeField] int whenTrue = -1;
        [SerializeField] int whenFalse = -1;

        public ConditionRuntimeNode(DialectCondition condition, int whenTrue, int whenFalse)
        {
            this.condition = condition;
            this.whenTrue = whenTrue;
            this.whenFalse = whenFalse;
        }

        public override DialectExecutionResult Execute(DialectExecutionContext context) =>
            DialectExecutionResult.ContinueTo(condition != null && condition.Evaluate(context) ? whenTrue : whenFalse);
    }
}
