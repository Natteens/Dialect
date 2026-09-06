using System;
using Dialect.Executors;
using Dialect.Values;

namespace Dialect.Samples.Complete
{
    [Serializable]
    public sealed class SamplePlayerNameResolver : DialectValueResolver
    {
        public override Type ValueType => typeof(string);
        public override object Resolve(DialectExecutionContext context) =>
            context.TryGetUserData<SampleState>(out var state) ? state.PlayerName : "Player";
    }
}
