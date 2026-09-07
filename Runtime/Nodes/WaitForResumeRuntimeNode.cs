using System;
using Dialect.Core;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class WaitForResumeRuntimeNode : RuntimeNode
    {
        [SerializeField] int next = -1;

        public WaitForResumeRuntimeNode(int next) => this.next = next;

        public override DialectExecutionResult Execute(DialectExecutionContext context) =>
            DialectExecutionResult.SuspendTo(next);
    }
}
