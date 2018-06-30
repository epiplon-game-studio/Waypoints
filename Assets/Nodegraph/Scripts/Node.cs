using System.Collections.Generic;
using UnityEngine;

namespace Nodegraph
{
    [System.Serializable]
    public struct Node
    {
        public Vector3 Position;
        public List<Node> ConnectedNodes;

        public Node(Vector3 position)
        {
            Position = position;
            ConnectedNodes = new List<Node>();
        }

        public static bool operator ==(Node n1, Node n2)
        {
            return n1.Equals(n2);
        }

        public static bool operator !=(Node n1, Node n2)
        {
            return !n1.Equals(n2);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
