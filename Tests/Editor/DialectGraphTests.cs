using System.Linq;
using Dialect.Core;
using Dialect.Editor;
using Dialect.Editor.Nodes;
using NUnit.Framework;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace Dialect.Tests.Editor
{
    public sealed class DialectGraphTests
    {
        const string TestPath = "Assets/__DialectGraphTest.dlg";

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(TestPath);

        [Test]
        public void EmptyGraphCreatesConnectedStartAndEnd()
        {
            var graph = DialectGraph.CreateInitialized(TestPath);
            var start = graph.GetNodes().OfType<StartNode>().Single();
            Assert.That(graph.GetNodes().OfType<EndNode>().Count(), Is.EqualTo(1));
            Assert.That(start.GetOutputPortByName(DialectNode.FlowOutput).IsConnected, Is.True);

            AssetDatabase.ImportAsset(TestPath, ImportAssetOptions.ForceSynchronousImport);
            var runtime = AssetDatabase.LoadAssetAtPath<DialectRuntimeGraph>(TestPath);
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsValid, Is.True, string.Join("\n", runtime.Diagnostics));
        }
    }
}
