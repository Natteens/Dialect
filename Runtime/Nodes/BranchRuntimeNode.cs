using System;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Values;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class BranchRuntimeNode : RuntimeNode
    {
        [SerializeField] DialectValueExpression condition;
        [SerializeField] int whenTrue = -1;
        [SerializeField] int whenFalse = -1;

        public BranchRuntimeNode(DialectValueExpression condition, int whenTrue, int whenFalse)
        {
            this.condition = condition;
            this.whenTrue = whenTrue;
            this.whenFalse = whenFalse;
        }

        public override DialectExecutionResult Execute(DialectExecutionContext context) =>
            DialectExecutionResult.ContinueTo(condition.Resolve(context) is true ? whenTrue : whenFalse);
    }
}
