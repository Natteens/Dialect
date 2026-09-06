using System;
using Dialect.Core;
using Dialect.Executors;

namespace Dialect.Nodes
{
    [Serializable]
    public sealed class EndRuntimeNode : RuntimeNode
    {
        public override DialectExecutionResult Execute(DialectExecutionContext context) => DialectExecutionResult.End();
    }
}
