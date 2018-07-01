using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nodegraph
{
    [System.Serializable]
    public class Node
    {
        public Vector3 Position;
        public List<int> ConnectedNodes;

        public Node(Vector3 position)
        {
            Position = position;
            ConnectedNodes = new List<int>();
        }
    }
}
