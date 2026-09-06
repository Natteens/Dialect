using System;
using Dialect.Core;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class StartRuntimeNode : RuntimeNode
    {
        [SerializeField] int next = -1;

        public StartRuntimeNode(int next) => this.next = next;

        public override DialectExecutionResult Execute(DialectExecutionContext context) =>
            DialectExecutionResult.ContinueTo(next);
    }
}
