using System;
using System.Collections.Generic;
using Dialect.Core;
using Dialect.Editor.Utils;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;
using UnityEngine.Localization;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    internal class ChoiceNode : BaseNode, IConvertibleToRuntime
    {
        const string PORT_COUNT_OPTION = "portCount";
        const string CHOICE_PORT_PREFIX = "Choice";

        protected override void OnDefineOptions(IOptionDefinitionContext context)
        {
            context.AddOption(
                name: PORT_COUNT_OPTION,
                dataType: typeof(int)
            ).WithDisplayName(displayName: "Choice Count").WithTooltip("Number of choices").WithDefaultValue(2);
        }

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputContextPort(context, EXECUTION_PORT_DEFAULT_NAME, INPUT_DISPLAY_NAME);
            
            int portCount = 2;
            GetNodeOptionByName(PORT_COUNT_OPTION)?.TryGetValue(out portCount);
            portCount = Math.Max(0, Math.Min(portCount, 6));

            for (int i = 0; i < portCount; i++)
            {
                string outPortName = $"{CHOICE_PORT_PREFIX}{i}_Out";
                string inPortName = $"{CHOICE_PORT_PREFIX}{i}_In";
                AddOutputContextPort(context, outPortName, $"Choice {i + 1} Out");
                AddInputContextPort<string>(context, inPortName, $"Choice {i + 1} In");
            }
        }

        public RuntimeNode CreateRuntimeNode()
        {
            var choiceTexts = new List<string>();
            var choiceLocalized = new List<LocalizedString>();
    
            int portCount = 2;
            GetNodeOptionByName(PORT_COUNT_OPTION)?.TryGetValue(out portCount);
    
            for (int i = 0; i < portCount; i++)
            {
                var choicePort = GetInputPortByName($"{CHOICE_PORT_PREFIX}{i}_In");
        
                if (choicePort != null && NodeUtility.IsPortConnected(choicePort) && NodeUtility.GetFirstConnectedPort(choicePort).GetNode() is LocalizedNode)
                {
                    var localizedNode = (LocalizedNode)NodeUtility.GetFirstConnectedPort(choicePort).GetNode();
                    var localizedPort = localizedNode.GetInputPortByName("localized");
                    choiceLocalized.Add(NodeUtility.GetInputPortValue<LocalizedString>(localizedPort));
                    choiceTexts.Add(null);
                }
                else
                {
                    var choiceText = NodeUtility.GetInputPortValue<string>(choicePort);
                    choiceTexts.Add(choiceText);
                    choiceLocalized.Add(null);
                }
            }
    
            return new ChoiceRuntimeNode
            {
                choiceTexts = choiceTexts.ToArray(),
                _choiceLocalized = choiceLocalized.ToArray()
            };
        }
    }
}
