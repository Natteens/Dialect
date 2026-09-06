using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Editor;
using Dialect.Editor.Nodes;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Dialect.Samples.Complete.Editor
{
    public static class CreateCompleteDemoAssets
    {
        const string Folder = "Assets/Dialect Complete Demo";

        [MenuItem("Tools/Dialect/Create Complete Demo Assets")]
        public static void Create()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(Folder);
                return;
            }
            EnsureFolder();
            var board = ScriptableObject.CreateInstance<DialectBlackboard>();
            var speaker = board.AddVariable("GuideName", new DialectStringValue("Guide"));
            AssetDatabase.CreateAsset(board, Folder + "/Demo Blackboard.asset");

            var action = ScriptableObject.CreateInstance<SetAcceptedAction>();
            var condition = ScriptableObject.CreateInstance<IsAcceptedCondition>();
            AssetDatabase.CreateAsset(action, Folder + "/Set Accepted.asset");
            AssetDatabase.CreateAsset(condition, Folder + "/Is Accepted.asset");

            var localized = CreateLocalizedLine();
            CreateGreeting(board, speaker, action, condition, localized);
            CreateGoodbye(board, speaker);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(Folder + "/Greeting.dlg");
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Dialect Complete Demo");
        }

        static LocalizedString CreateLocalizedLine()
        {
            var locales = LocalizationEditorSettings.GetLocales();
            if (locales.Count == 0) return null;
            var collection = LocalizationEditorSettings.CreateStringTableCollection("Dialect Demo", Folder, locales);
            foreach (var localizationTable in collection.StringTables)
            {
                localizationTable.AddEntry("welcome", localizationTable.LocaleIdentifier.Code == "pt-BR"
                    ? "Bem-vindo ao Dialect."
                    : "Welcome to Dialect.");
                EditorUtility.SetDirty(localizationTable);
            }
            EditorUtility.SetDirty(collection.SharedData);
            return new LocalizedString(collection.SharedData.TableCollectionNameGuid, "welcome");
        }

        static void CreateGreeting(DialectBlackboard board, DialectVariableDefinition speaker,
            SetAcceptedAction action, IsAcceptedCondition condition, LocalizedString localized)
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(Folder + "/Greeting.dlg");
            graph.LinkBlackboard(board);
            var start = new StartNode { Position = new Vector2(80, 180) };
            var shared = new SharedVariableNode { Position = new Vector2(280, 40) };
            var first = new DialogueNode { Position = new Vector2(360, 180) };
            var dynamicName = new SamplePlayerNameNode { Position = new Vector2(560, 40) };
            var second = new DialogueNode { Position = new Vector2(650, 180) };
            var choice = new ChoiceNode { Position = new Vector2(930, 180) };
            var branch = new ConditionNode { Position = new Vector2(1210, 120) };
            var runAction = new ActionNode { Position = new Vector2(1490, 80) };
            var end = new EndNode { Position = new Vector2(1760, 180) };
            graph.AddNode(start);
            graph.AddNode(shared);
            graph.AddNode(first);
            graph.AddNode(dynamicName);
            graph.AddNode(second);
            graph.AddNode(choice);
            graph.AddNode(branch);
            graph.AddNode(runAction);
            graph.AddNode(end);

            shared.GetInputPortByName(SharedVariableNode.ReferencePort)
                .TrySetValue(new DialectVariableReference(board, speaker.Id));
            graph.Connect(shared.GetOutputPortByName(DialectValueNode.ValueOutput),
                first.GetInputPortByName(DialogueNode.SpeakerPort));
            var localLine = graph.CreateVariable("GreetingLine", "The local variable is connected to this line.", VariableKind.Local);
            first.GetInputPortByName(DialogueNode.TextPort).TrySetValue(DialectText.Inline("A local value can drive a dialogue speaker."));
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), first.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(first.GetOutputPortByName(DialectNode.FlowOutput), second.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(dynamicName.GetOutputPortByName(DialectValueNode.ValueOutput),
                second.GetInputPortByName(DialogueNode.SpeakerPort));
            second.GetInputPortByName(DialogueNode.TextPort).TrySetValue(localized == null
                ? DialectText.Inline("Welcome to Dialect.")
                : DialectText.Localized(localized));
            graph.Connect(second.GetOutputPortByName(DialectNode.FlowOutput), choice.GetInputPortByName(DialectNode.FlowInput));
            choice.GetInputPortByName(ChoiceNode.TextName(0)).TrySetValue(DialectText.Inline("Accept"));
            choice.GetInputPortByName(ChoiceNode.TextName(1)).TrySetValue(DialectText.Inline("Leave"));
            graph.Connect(choice.GetOutputPortByName(ChoiceNode.TargetName(0)), branch.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(choice.GetOutputPortByName(ChoiceNode.TargetName(1)), end.GetInputPortByName(DialectNode.FlowInput));
            branch.GetInputPortByName("condition").TrySetValue<Dialect.Conditions.DialectCondition>(condition);
            graph.Connect(branch.GetOutputPortByName("true"), runAction.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(branch.GetOutputPortByName("false"), end.GetInputPortByName(DialectNode.FlowInput));
            runAction.GetInputPortByName("action").TrySetValue<Dialect.Actions.DialectAction>(action);
            graph.Connect(runAction.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
        }

        static void CreateGoodbye(DialectBlackboard board, DialectVariableDefinition speaker)
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(Folder + "/Goodbye.dlg");
            graph.LinkBlackboard(board);
            var start = new StartNode { Position = new Vector2(80, 180) };
            var shared = new SharedVariableNode { Position = new Vector2(260, 40) };
            var dialogue = new DialogueNode { Position = new Vector2(360, 180) };
            var end = new EndNode { Position = new Vector2(650, 180) };
            graph.AddNode(start);
            graph.AddNode(shared);
            graph.AddNode(dialogue);
            graph.AddNode(end);
            shared.GetInputPortByName(SharedVariableNode.ReferencePort)
                .TrySetValue(new DialectVariableReference(board, speaker.Id));
            graph.Connect(shared.GetOutputPortByName(DialectValueNode.ValueOutput),
                dialogue.GetInputPortByName(DialogueNode.SpeakerPort));
            dialogue.GetInputPortByName(DialogueNode.TextPort).TrySetValue(DialectText.Inline("Goodbye."));
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), dialogue.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(dialogue.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
        }
    }
}
