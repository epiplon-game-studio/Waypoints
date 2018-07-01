using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
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

        [Header("Node Settings")]
        public float m_nodeElevation = 1f;
        public float m_nodeMaximumDistance = 3f;
        public float m_nodeSize = 0.25f;
        [Tooltip("For removing nodes")]
        public float m_brushRadius = 3f;

        [Header("Editor")]
        public bool m_alwaysShowNodes;
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

            var newNode = new Node(position + Vector3.up * m_nodeElevation);
            graph.AllNodes.Add(newNode);
        }

        public void ClearNodes()
        {
            graph.AllNodes.Clear();
        }

        public void RemoveNodes(Vector3 center)
        {
            if (graph == null)
            {
                Debug.LogError("No Graph is set on the " + gameObject.name + " object.");
                return;
            }

            if (graph.AllNodes.Count == 0)
            {
                Debug.LogWarning("No nodes to remove.");
                return;
            }

            graph.AllNodes.RemoveAll(n => Vector3.Distance(n.Position, center) <= m_brushRadius);
        }

        public void RebuildNodegraph()
        {
            for (int i = 0; i < graph.AllNodes.Count; i++)
            {
                var overlapped = Physics.OverlapSphere(graph.AllNodes[i].Position, 0.1f, solidLayerMask);
                if (overlapped.Count() > 0)
                {
                    graph.AllNodes[i].ConnectedNodes.Clear();
                    Debug.Log("Found overlap");
                }
                else
                {
                    var connectedNodes = graph.AllNodes.Where(n => graph.AllNodes[i] != n)
                        .Where(n =>
                        {
                            if (Vector3.Distance(graph.AllNodes[i].Position, n.Position) < m_nodeMaximumDistance)
                            {
                                // don't connect if has obstacle
                                bool hasObstacle = Physics.Linecast(graph.AllNodes[i].Position, n.Position, solidLayerMask);
                                return !hasObstacle;
                            }
                            else return false;
                        })
                        .Select(n => graph.AllNodes.IndexOf(n));

                    graph.AllNodes[i].ConnectedNodes = connectedNodes.ToList();
                }

            }
        }

        public List<Node> QueryPath(Vector3 start, Vector3 end)
        {
            if (graph == null || graph.AllNodes.Count == 0)
                return new List<Node>();

            DateTime clockStart = DateTime.Now;
            //==============

            float startMin = graph.AllNodes.Min(n => Vector3.Distance(start, n.Position));
            Node startNode = graph.AllNodes.Find(n => Vector3.Distance(start, n.Position) <= startMin);

            float endMin = graph.AllNodes.Min(n => Vector3.Distance(end, n.Position));
            Node endNode = graph.AllNodes.Find(n => Vector3.Distance(end, n.Position) <= endMin);

            List<Node> path = new List<Node>();
            List<Node> visited = new List<Node>();
            path = Search(startNode, endNode, ref path, ref visited);

            //=============
            TimeSpan clockEnd = DateTime.Now - clockStart;
            Debug.Log("Operation took: " + clockEnd.TotalSeconds);
            Debug.Log("Search found " + path.Count + " nodes.");

            return path;
        }

        private List<Node> Search(Node node, Node lastNode, ref List<Node> path, ref List<Node> visited)
        {
            path.Add(node);
            visited.Add(node);

            foreach (var index in node.ConnectedNodes)
            {
                Node child = graph.GetNode(index);

                // don't let it go to the same path
                if (visited.Contains(child))
                    continue;
                else
                    path = Search(child, lastNode, ref path, ref visited);
            }

            if (path.Last() != lastNode)
                path.RemoveAt(path.Count - 1);

            return path;
        }


        #endregion

#if UNITY_EDITOR
        [HideInInspector]
        public NodegraphState State = NodegraphState.None;

        private void OnDrawGizmosSelected()
        {
            if (m_alwaysShowNodes)
                return;

            DrawNodegraph();
        }

        private void OnDrawGizmos()
        {
            if (m_alwaysShowNodes)
                DrawNodegraph();
        }

        void DrawNodegraph()
        {
            if (graph)
            {
                if (graph.AllNodes != null)
                {
                    foreach (var n in graph.AllNodes)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawCube(n.Position, Vector3.one * m_nodeSize);
                        foreach (var c in n.ConnectedNodes)
                        {
                            var node = graph.GetNode(c);
                            if (node != null)
                                Gizmos.DrawLine(n.Position, node.Position);

                        }
                    }
                }

            }
        }
#endif
    }

    public enum NodegraphState
    {
        None,
        Placing,
        Removing
    }
}
