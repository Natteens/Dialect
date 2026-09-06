using Dialect.Conditions;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Samples.Complete
{
    [CreateAssetMenu(menuName = "Dialect Samples/Is Accepted")]
    public sealed class IsAcceptedCondition : DialectCondition
    {
        public override bool Evaluate(DialectExecutionContext context) =>
            context.TryGetUserData<SampleState>(out var state) && state.Accepted;
    }
}
