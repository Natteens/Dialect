using Dialect.Actions;
using Dialect.Executors;
using UnityEngine;

namespace Dialect.Samples.Complete
{
    [CreateAssetMenu(menuName = "Dialect Samples/Set Accepted")]
    public sealed class SetAcceptedAction : DialectAction
    {
        [SerializeField] bool value = true;

        public override void Execute(DialectExecutionContext context)
        {
            if (context.TryGetUserData<SampleState>(out var state)) state.Accepted = value;
        }
    }
}
