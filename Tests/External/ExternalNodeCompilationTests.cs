using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Editor.Nodes;
using Dialect.Executors;
using Dialect.Values;
using NUnit.Framework;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using Dialect.Editor;

namespace Dialect.ExternalTests
{
    [Serializable, Node("External Tests", null, "External Node"), UseWithGraph(typeof(DialectGraph))]
    public sealed class ExternalNode : DialectNode, IDialectNodeCompiler
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        { AddFlowInput(context); AddFlowOutput(context); }
        public RuntimeNode Compile(DialectNodeCompilationContext context) => new ExternalRuntimeNode(context.Target(FlowOutput));
        public void Validate(DialectNodeValidationContext context) { }
        public bool WaitsForInput => false;
    }

    [Serializable]
    public sealed class ExternalRuntimeNode : RuntimeNode
    {
        [SerializeField] int next;
        public ExternalRuntimeNode(int next) => this.next = next;
        public override DialectExecutionResult Execute(DialectExecutionContext context) => DialectExecutionResult.ContinueTo(next);
    }

    [Serializable, Node("External Tests", null, "External Text"), UseWithGraph(typeof(DialectGraph))]
    public sealed class ExternalTextNode : DialectValueNode, IDialectValueNodeCompiler
    {
        protected override void OnDefinePorts(IPortDefinitionContext context) => AddValueOutput<DialectText>(context);
        public Type ValueType => typeof(string);
        public DialectValueResolver CompileValue(DialectValueNodeCompilationContext context) => new ExternalTextResolver();
        public void Validate(DialectValueNodeValidationContext context) { }
    }

    [Serializable]
    public sealed class ExternalTextResolver : DialectValueResolver
    {
        public override Type ValueType => typeof(string);
        public override object Resolve(DialectExecutionContext context) => "External Speaker";
    }

    public sealed class ExternalNodeCompilationTests
    {
        const string TestPath = "Assets/__DialectExternalNodeTest.dlg";

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(TestPath);

        [SetUp]
        public void SetUp() => AssetDatabase.DeleteAsset(TestPath);

        [Test]
        public void PublicSdkNodeCompilesAndExecutesFromIndependentAssembly()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var external = new ExternalNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(external);
            graph.AddNode(end);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), external.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(external.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);

            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
            Assert.That(runtime.Nodes, Has.Some.InstanceOf<ExternalRuntimeNode>());

            var owner = new GameObject("External Dialect Test");
            try
            {
                var director = owner.AddComponent<DialectDirector>();
                director.Play(runtime);
                Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void PublicValueNodeFeedsDialogueWithoutImporterChanges()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var value = new ExternalTextNode();
            var dialogue = new DialogueNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(value);
            graph.AddNode(dialogue);
            graph.AddNode(end);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), dialogue.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(value.GetOutputPortByName(DialectValueNode.ValueOutput), dialogue.GetInputPortByName(DialogueNode.SpeakerPort));
            dialogue.GetInputPortByName(DialogueNode.TextPort).TrySetValue(DialectText.Inline("Hello"));
            graph.Connect(dialogue.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);

            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));

            var owner = new GameObject("External Dialect Value Test");
            try
            {
                DialectLine line = default;
                var director = owner.AddComponent<DialectDirector>();
                director.LinePresented += value => line = value;
                director.Play(runtime);
                Assert.That(line.Speaker, Is.EqualTo("External Speaker"));
                Assert.That(line.Text, Is.EqualTo("Hello"));
                Assert.That(director.Advance(), Is.True);
                Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void PublicValueNodeFeedsChoiceTextAndExecutesToEnd()
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(TestPath);
            var start = new StartNode();
            var value = new ExternalTextNode();
            var choice = new ChoiceNode();
            var end = new EndNode();
            graph.AddNode(start);
            graph.AddNode(value);
            graph.AddNode(choice);
            graph.AddNode(end);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), choice.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(value.GetOutputPortByName(DialectValueNode.ValueOutput), choice.GetInputPortByName(ChoiceNode.TextName(0)));
            choice.GetInputPortByName(ChoiceNode.TextName(1)).TrySetValue(DialectText.Inline("Leave"));
            graph.Connect(choice.GetOutputPortByName(ChoiceNode.TargetName(0)), end.GetInputPortByName(DialectNode.FlowInput));
            graph.Connect(choice.GetOutputPortByName(ChoiceNode.TargetName(1)), end.GetInputPortByName(DialectNode.FlowInput));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);

            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
            var owner = new GameObject("External Dialect Choice Value Test");
            try
            {
                DialectChoiceSet choices = null;
                var director = owner.AddComponent<DialectDirector>();
                director.ChoicesPresented += valueSet => choices = valueSet;
                director.Play(runtime);
                Assert.That(choices.Choices[0].Text, Is.EqualTo("External Speaker"));
                Assert.That(director.Choose(0), Is.True);
                Assert.That(director.Session.TerminationReason, Is.EqualTo(DialectTerminationReason.Completed));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}
