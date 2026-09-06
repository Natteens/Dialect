using System.Collections.Generic;
using Dialect.Core;
using Unity.GraphToolkit.Editor.GraphVisualization;
using UnityEditor;
using UnityEngine;

namespace Dialect.Editor.Visualization
{
    [InitializeOnLoad]
    static class DialectRuntimeVisualization
    {
        static readonly List<DialectDirector> Directors = new();

        static DialectRuntimeVisualization() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Directors.Clear();
                Directors.AddRange(Object.FindObjectsByType<DialectDirector>(FindObjectsInactive.Include));
                foreach (var director in Directors)
                {
                    director.NodeEntered += OnNodeEntered;
                    director.SessionEnded += OnSessionEnded;
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                foreach (var director in Directors)
                    if (director != null) director.NodeEntered -= OnNodeEntered;
                foreach (var director in Directors)
                    if (director != null) director.SessionEnded -= OnSessionEnded;
                Directors.Clear();
            }
        }

        static void OnNodeEntered(DialectSession session, RuntimeNode node)
        {
            Hash128 graphId;
            Hash128 nodeId;
            try { graphId = Hash128.Parse(session.Graph.GraphId); nodeId = Hash128.Parse(node.AuthoringId); }
            catch { return; }
            var context = Registry.GetActiveContext(graphId);
            if (context == null || !context.IsValid || !context.IsGraphLoaded) return;
            context.NodeCustomizationEnabled = true;
            context.ClearAllVisualization();
            var reference = context.GetNodeReference(nodeId);
            reference.FillAmount = 1f;
            context.Motion.Play(reference, .35f);
        }

        static void OnSessionEnded(DialectSession session, DialectTerminationReason reason) => Clear(session?.Graph);

        static void Clear(DialectRuntimeGraph graph)
        {
            if (graph == null) return;
            Hash128 graphId;
            try { graphId = Hash128.Parse(graph.GraphId); }
            catch { return; }
            var context = Registry.GetActiveContext(graphId);
            if (context != null && context.IsValid) context.ClearAllVisualization();
        }
    }
}
