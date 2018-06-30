using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nodegraph
{
    [ExecuteInEditMode]
    public class Nodegraph : MonoBehaviour
    {
        public static Nodegraph Current
        {
            get
            {
                if (_current == null)
                {
                    _current = FindObjectOfType<Nodegraph>();
                    if (_current == null)
                    {
                        var nodegraph = new GameObject("NODEGRAPH");
                        _current = nodegraph.AddComponent<Nodegraph>();
                    }
                }

                return _current;
            }
        }
        static Nodegraph _current;

        public Graph graph;
        public float m_nodeElevation = 1f;
        public float m_nodeMaximumDistance = 3f;

        [Header("Editor")]
        public LayerMask solidLayerMask;

        #region Node Operations

        public void AddNode(Vector3 position)
        {
            if (graph == null)
            {
                Debug.LogError("No Graph is set on the " + gameObject.name + " object.");
                return;
            }

            if (graph.AllNodes == null)
                graph.AllNodes = new List<Node>();


            graph.AllNodes.Add(new Node(position + Vector3.up * m_nodeElevation));
        }

        public void ClearNodes()
        {
            graph.AllNodes.Clear();
        }

        public void RebuildNodegraph()
        {
            for (int i = 0; i < graph.AllNodes.Count; i++)
            {
                for (int j = 0; j < graph.AllNodes.Count; j++)
                {
                    if (graph.AllNodes[i] == graph.AllNodes[j])
                        continue;

                    var distance = Vector3.Distance(graph.AllNodes[i].Position, graph.AllNodes[j].Position);
                    if (distance < m_nodeMaximumDistance)
                    {
                        graph.AllNodes[i].ConnectedNodes.Add(graph.AllNodes[j]);
                    }
                }
            }
        }

        #endregion

#if UNITY_EDITOR
        [HideInInspector]
        public bool isPlacingNode = false;

        private void OnDrawGizmosSelected()
        {
            if (graph)
            {
                if (graph.AllNodes != null)
                {
                    foreach (var n in graph.AllNodes)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawCube(n.Position, Vector3.one / 4f);
                        foreach (var c in n.ConnectedNodes)
                        {
                            Gizmos.DrawLine(n.Position, c.Position);
                        }
                    }
                }

            }
        }
#endif
    }
}
