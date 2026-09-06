using System;
using Dialect.Core;
using Dialect.Editor.Nodes;
using Dialect.Executors;
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

    public sealed class ExternalNodeCompilationTests
    {
        const string TestPath = "Assets/__DialectExternalNodeTest.dlg";

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(TestPath);

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
    }
}
