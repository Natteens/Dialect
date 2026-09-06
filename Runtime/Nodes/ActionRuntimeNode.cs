using System;
using Dialect.Actions;
using Dialect.Core;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class ActionRuntimeNode : RuntimeNode
    {
        [SerializeField] DialectAction action;
        [SerializeField] int next = -1;

        public ActionRuntimeNode(DialectAction action, int next)
        {
            this.action = action;
            this.next = next;
        }

        public override DialectExecutionResult Execute(DialectExecutionContext context)
        {
            action?.Execute(context);
            return DialectExecutionResult.ContinueTo(next);
        }
    }
}
