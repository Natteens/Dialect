using System;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Values;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class WaitUntilRuntimeNode : RuntimeNode
    {
        [SerializeField] DialectValueExpression condition;
        [SerializeField] int next = -1;

        public WaitUntilRuntimeNode(DialectValueExpression condition, int next)
        {
            this.condition = condition;
            this.next = next;
        }

        public override DialectExecutionResult Execute(DialectExecutionContext context)
        {
            if (condition.Resolve(context) is not bool ready)
                throw new InvalidOperationException("Wait Until Condition must resolve to Boolean.");
            if (ready) return DialectExecutionResult.ContinueTo(next);
            context.Director.BeginWaitUntil(context.Session, condition, context, next);
            return DialectExecutionResult.SuspendAutomaticallyTo(next);
        }
    }
}
