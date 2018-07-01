using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nodegraph;

public class FindPlayer : MonoBehaviour
{
    GameObject Player;
    List<Node> Path;

    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 100, 40), "Find"))
        {
            Path = Nodegraph.Nodegraph.Current.QueryPath(transform.position, Player.transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        if (Path != null)
        {
            for (int i = 0; i < Path.Count; i++)
            {
                if (i > 0)
                    Gizmos.DrawLine(Path[i - 1].Position, Path[i].Position);

                Gizmos.DrawCube(Path[i].Position, Vector3.one * 0.5f);

            }
        }
    }
}
