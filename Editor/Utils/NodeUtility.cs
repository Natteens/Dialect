using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Utils
{
    /// <summary>
    /// Utility methods for editor nodes
    /// </summary>
    internal static class NodeUtility
    {
        public static int GetOutputPortCount(INode node)
        {
#if UNITY_6000_4_OR_NEWER
            return node.OutputPortCount;
#else
            return node.outputPortCount;
#endif
        }

        public static bool IsPortConnected(IPort port)
        {
#if UNITY_6000_4_OR_NEWER
            return port.IsConnected;
#else
            return port.isConnected;
#endif
        }

        public static IPort GetFirstConnectedPort(IPort port)
        {
#if UNITY_6000_4_OR_NEWER
            return port.FirstConnectedPort;
#else
            return port.firstConnectedPort;
#endif
        }

        public static IVariable GetVariable(IVariableNode node)
        {
#if UNITY_6000_4_OR_NEWER
            return node.Variable;
#else
            return node.variable;
#endif
        }

        /// <summary>
        /// Gets the value from an input port, checking connected nodes or the port's own value
        /// </summary>
        public static T GetInputPortValue<T>(IPort port)
        {
            T value = default;

            if (port == null) return value;

            if (IsPortConnected(port))
            {
                switch (GetFirstConnectedPort(port).GetNode())
                {
                    case IVariableNode variableNode:
                        GetVariable(variableNode).TryGetDefaultValue(out value);
                        break;
                    case IConstantNode constantNode:
                        constantNode.TryGetValue<T>(out value);
                        break;
                }
            }
            else
            {
                port.TryGetValue(out value);
            }
            
            return value;
        }
    }
}
