using Dialect.Blackboards;
using Dialect.Core;

namespace Dialect.Executors
{
    public sealed class DialectExecutionContext
    {
        internal DialectExecutionContext(DialectDirector director, DialectRuntimeGraph graph,
            DialectSession session, DialectVariableStore variables, object userData)
        {
            Director = director;
            Graph = graph;
            Session = session;
            Variables = variables;
            UserData = userData;
        }

        public DialectDirector Director { get; }
        public DialectRuntimeGraph Graph { get; }
        public DialectSession Session { get; }
        public DialectVariableStore Variables { get; }
        public object UserData { get; }

        public bool TryGetUserData<T>(out T value)
        {
            if (UserData is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public void ReportResolvedValue(string portId, string value)
        {
            if (!string.IsNullOrEmpty(portId)) Director.ReportResolvedValue(portId, value);
        }
    }
}
