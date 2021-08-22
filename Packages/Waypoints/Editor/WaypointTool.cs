using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Waypoints.Editor
{
    [EditorTool("Waypoints")]
    public class WaypointTool : EditorTool
    {
        public override void OnToolGUI(EditorWindow window)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 position = Tools.handlePosition;

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = position - Tools.handlePosition;

                Undo.RecordObjects(Selection.transforms, "Move Platform");

                foreach (var transform in Selection.transforms)
                    transform.position += delta;
            }

        }
    }
}
