using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Utils
{
    public static class NodeUtility
    {
        public static int GetOutputPortCount(INode node)
        {
            return node.OutputPortCount;
        }

        public static bool IsPortConnected(IPort port)
        {
            return port.IsConnected;
        }

        public static IPort GetFirstConnectedPort(IPort port)
        {
            return port.FirstConnectedPort;
        }

        public static IVariable GetVariable(IVariableNode node)
        {
            return node.Variable;
        }

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
