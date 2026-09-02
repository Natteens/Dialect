using System;
using Dialect.Core;
using Dialect.Editor.Utils;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;
using UnityEngine.Localization;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    internal class DialogueNode : BaseNode, IConvertibleToRuntime
    {
        const string SPEAKER_NAME_PORT = "SpeakerName";
        const string DIALOGUE_TEXT_PORT = "DialogueText";
        const string SPEAKER_NAME_DISPLAY = "Speaker Name";
        const string DIALOGUE_TEXT_DISPLAY = "Dialogue Text";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            AddInputContextPort(context, EXECUTION_PORT_DEFAULT_NAME, INPUT_DISPLAY_NAME);
            AddOutputContextPort(context, EXECUTION_PORT_DEFAULT_NAME, OUTPUT_DISPLAY_NAME);
            AddInputContextPort<string>(context, SPEAKER_NAME_PORT, SPEAKER_NAME_DISPLAY);
            AddInputContextPort<string>(context, DIALOGUE_TEXT_PORT, DIALOGUE_TEXT_DISPLAY);
        }

        public RuntimeNode CreateRuntimeNode()
        {
            var speakerPort = GetInputPortByName(SPEAKER_NAME_PORT);
            var dialoguePort = GetInputPortByName(DIALOGUE_TEXT_PORT);
    
            var runtimeNode = new DialogueRuntimeNode();
    
            if (speakerPort != null && NodeUtility.IsPortConnected(speakerPort) && NodeUtility.GetFirstConnectedPort(speakerPort).GetNode() is LocalizedNode)
            {
                var localizedNode = (LocalizedNode)NodeUtility.GetFirstConnectedPort(speakerPort).GetNode();
                var localizedPort = localizedNode.GetInputPortByName("localized");
                runtimeNode._speakerLocalized = NodeUtility.GetInputPortValue<LocalizedString>(localizedPort);
            }
            else
            {
                runtimeNode.speakerName = NodeUtility.GetInputPortValue<string>(speakerPort);
            }
    
            if (dialoguePort != null && NodeUtility.IsPortConnected(dialoguePort) && NodeUtility.GetFirstConnectedPort(dialoguePort).GetNode() is LocalizedNode)
            {
                var localizedNode = (LocalizedNode)NodeUtility.GetFirstConnectedPort(dialoguePort).GetNode();
                var localizedPort = localizedNode.GetInputPortByName("localized");
                runtimeNode._dialogueLocalized = NodeUtility.GetInputPortValue<LocalizedString>(localizedPort);
            }
            else
            {
                runtimeNode.dialogueText = NodeUtility.GetInputPortValue<string>(dialoguePort);
            }
    
            return runtimeNode;
        }
    }
}
