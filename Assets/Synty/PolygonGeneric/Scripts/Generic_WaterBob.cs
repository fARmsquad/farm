using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generic_WaterBob : MonoBehaviour
{
    public float bobbingHeight = 0.08f; // The height the object will bob up and down
    public float bobbingSpeed = 1.5f; // The speed of the bobbing motion
    public float rotationAmount = 0.8f; // The amount of rotation applied to the object
    public bool randomOffset = true; // Determines if random offsets are applied to speed and rotation
    public Vector2 randomRange = new Vector2(0.1f, 1f); // The range for the random offset
    private Vector3 startPos;
    private Quaternion startRotation;

    void Start()
    {
        startPos = transform.position;
        startRotation = transform.rotation; // Save the initial rotation

        if (randomOffset)
        {
            bobbingSpeed += UnityEngine.Random.Range(randomRange.x, randomRange.y);
            rotationAmount += UnityEngine.Random.Range(randomRange.x, randomRange.y);
        }
    }

    void Update()
    {
        // Calculate the vertical bobbing motion
        float newY = startPos.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        Vector3 newPos = new Vector3(transform.position.x, newY, transform.position.z);
        transform.position = newPos;

        // Calculate rotation offsets based on time
        float rotationX = Mathf.Sin(Time.time * bobbingSpeed * 0.5f) * rotationAmount;
        float rotationY = Mathf.Sin(Time.time * bobbingSpeed * 0.7f) * rotationAmount;
        float rotationZ = Mathf.Sin(Time.time * bobbingSpeed * 0.9f) * rotationAmount;

        // Apply the incremental rotation as an offset to the existing rotation
        Quaternion incrementalRotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
        transform.rotation = startRotation * incrementalRotation;
    }
}
