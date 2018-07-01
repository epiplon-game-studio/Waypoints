using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nodegraph
{
    [CreateAssetMenu(fileName = "GraphName", menuName = "Nodegraph/New Graph")]
    public class Graph : ScriptableObject
    {
        public List<Node> AllNodes;

        public Node GetNode(int index)
        {
            return AllNodes.ElementAtOrDefault(index);
        }
    }
}
