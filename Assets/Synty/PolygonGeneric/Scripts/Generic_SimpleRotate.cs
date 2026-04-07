using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generic_SimpleRotate : MonoBehaviour
{
    
    public bool rotX;
    public float rotXSpeed = 50f;
    public bool rotY;
    public float rotYSpeed = 50f;
    public bool rotZ;
    public float rotZSpeed = 50f;

    // Update is called once per frame
    void Update()
    {
        if (rotX == true)
        {
            transform.Rotate(Vector3.left * Time.deltaTime * rotXSpeed);
        }
        if (rotY == true)
        {
            transform.Rotate(Vector3.up * Time.deltaTime * rotYSpeed);
        }

        if (rotZ == true)
        {
            transform.Rotate(Vector3.back * Time.deltaTime * rotZSpeed);
        }

    }

  
}
