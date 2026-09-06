using System;

namespace Dialect.Core
{
    public enum DialectExecutionKind
    {
        Continue,
        WaitForAdvance,
        AwaitChoice,
        End,
        Suspended
    }

    [Serializable]
    public readonly struct DialectExecutionResult
    {
        DialectExecutionResult(DialectExecutionKind kind, int targetNodeIndex)
        {
            Kind = kind;
            TargetNodeIndex = targetNodeIndex;
        }

        public DialectExecutionKind Kind { get; }
        public int TargetNodeIndex { get; }

        public static DialectExecutionResult ContinueTo(int target) =>
            new(DialectExecutionKind.Continue, target);

        public static DialectExecutionResult WaitForAdvance(int target) =>
            new(DialectExecutionKind.WaitForAdvance, target);

        public static DialectExecutionResult AwaitChoice() =>
            new(DialectExecutionKind.AwaitChoice, -1);

        public static DialectExecutionResult End() => new(DialectExecutionKind.End, -1);
        public static DialectExecutionResult Suspended() => new(DialectExecutionKind.Suspended, -1);
    }
}
