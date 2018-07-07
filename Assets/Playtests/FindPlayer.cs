using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nodegraph;

public class FindPlayer : MonoBehaviour
{
    GameObject Player;
    List<Connection> Path;
    Nodegraph.Nodegraph debug;

    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        debug = Nodegraph.Nodegraph.Get("debug");
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 100, 40), "Find"))
        {
            Path = debug.QueryPath(transform.position, Player.transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        Node lastNode = null;
        if (Path != null)
        {
            for (int i = 0; i < Path.Count; i++)
            {

                Node node = debug.GetNode(Path[i].EndNodeIndex);
                if (i == 0)
                {
                    Gizmos.DrawLine(transform.position, node.Position);
                }
                else
                {
                    Gizmos.DrawLine(lastNode.Position, node.Position);
                }
                Gizmos.DrawCube(node.Position, Vector3.one * 0.5f);

                lastNode = node;
            }
        }
    }
}
