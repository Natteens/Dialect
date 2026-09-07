using System;
using Dialect.Core;
using Dialect.Executors;
using Dialect.Values;
using UnityEngine;

namespace Dialect.Nodes
{
    public enum WaitTimeMode { Scaled, Unscaled }

    [Serializable]
    public sealed class WaitRuntimeNode : RuntimeNode
    {
        [SerializeField] DialectValueExpression duration;
        [SerializeField] WaitTimeMode timeMode;
        [SerializeField] int next = -1;

        public WaitRuntimeNode(DialectValueExpression duration, WaitTimeMode timeMode, int next)
        {
            this.duration = duration;
            this.timeMode = timeMode;
            this.next = next;
        }

        public override DialectExecutionResult Execute(DialectExecutionContext context)
        {
            if (duration.Resolve(context) is not float seconds || float.IsNaN(seconds) || float.IsInfinity(seconds))
                throw new InvalidOperationException("Wait Duration must be a finite number.");
            if (timeMode is not WaitTimeMode.Scaled and not WaitTimeMode.Unscaled)
                throw new InvalidOperationException("Wait Time Mode is invalid.");
            if (seconds <= 0f) return DialectExecutionResult.ContinueTo(next);
            context.Director.BeginWait(context.Session, seconds, timeMode, next);
            return DialectExecutionResult.SuspendAutomaticallyTo(next);
        }
    }
}
