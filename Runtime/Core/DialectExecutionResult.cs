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
        DialectExecutionResult(DialectExecutionKind kind, int targetNodeIndex, DialectSuspensionKind suspensionKind = default)
        {
            Kind = kind;
            TargetNodeIndex = targetNodeIndex;
            SuspensionKind = suspensionKind;
        }

        public DialectExecutionKind Kind { get; }
        public int TargetNodeIndex { get; }
        internal DialectSuspensionKind SuspensionKind { get; }

        public static DialectExecutionResult ContinueTo(int target) =>
            new(DialectExecutionKind.Continue, target);

        public static DialectExecutionResult WaitForAdvance(int target) =>
            new(DialectExecutionKind.WaitForAdvance, target);

        public static DialectExecutionResult AwaitChoice() =>
            new(DialectExecutionKind.AwaitChoice, -1);

        public static DialectExecutionResult End() => new(DialectExecutionKind.End, -1);
        public static DialectExecutionResult Suspended() => new(DialectExecutionKind.Suspended, -1, DialectSuspensionKind.Legacy);
        public static DialectExecutionResult SuspendTo(int target) =>
            new(DialectExecutionKind.Suspended, target, DialectSuspensionKind.Manual);
        internal static DialectExecutionResult SuspendAutomaticallyTo(int target) =>
            new(DialectExecutionKind.Suspended, target, DialectSuspensionKind.Automatic);
    }

    enum DialectSuspensionKind { None, Legacy, Manual, Automatic }
}
