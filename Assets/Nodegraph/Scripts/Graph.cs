using System.Collections.Generic;
using UnityEngine;

namespace Nodegraph
{
    [CreateAssetMenu(fileName = "GraphName", menuName = "Nodegraph/New Graph")]
    public class Graph : ScriptableObject
    {
        [HideInInspector]
        public List<Node> AllNodes;
    }
}
