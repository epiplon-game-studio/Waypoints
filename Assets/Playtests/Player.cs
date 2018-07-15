using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Camera ownCamera;
    public float speed;

    void Start()
    {
        ownCamera = Camera.main;
    }
    
    void Update()
    {
        var horizontal = Input.GetAxis("Vertical") * ownCamera.transform.forward; 
        var vertical = Input.GetAxis("Horizontal") * ownCamera.transform.right;
        var direction = (horizontal + vertical).normalized;
        direction.y = 0;
        transform.position += direction * Time.deltaTime * speed;
    }
}
