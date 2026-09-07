using System;
using System.Collections.Generic;
using Dialect.Core;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class RandomBranchRuntimeNode : RuntimeNode
    {
        [SerializeField] List<int> targets = new();

        public RandomBranchRuntimeNode(List<int> targets) => this.targets = targets ?? new List<int>();

        public override DialectExecutionResult Execute(DialectExecutionContext context)
        {
            if (targets.Count == 0) throw new InvalidOperationException("Random Branch has no outputs.");
            return DialectExecutionResult.ContinueTo(targets[context.Session.NextRandom(targets.Count)]);
        }
    }
}
