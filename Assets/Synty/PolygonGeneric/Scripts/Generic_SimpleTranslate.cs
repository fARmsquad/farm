using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generic_SimpleTranslate : MonoBehaviour
{
    public bool moveX;
    public float moveXSpeed = 2f;
    public bool moveY;
    public float moveYSpeed = 2f;
    public bool moveZ;
    public float moveZSpeed = 2f;

    void Update()
    {
        if (moveX == true)
        {
            transform.Translate(Vector3.left * Time.deltaTime * moveXSpeed);
        }
        if (moveY == true)
        {
            transform.Translate(Vector3.up * Time.deltaTime * moveYSpeed);
        }

        if (moveZ == true)
        {
            transform.Translate(Vector3.back * Time.deltaTime * moveZSpeed);
        }
    }
}

