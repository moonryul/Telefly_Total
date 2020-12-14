using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FibonacciRays 
{
    public int numViewDirections = 10;
    public Vector3[] directions;

    // 'Fibonacci Rays' class
    public FibonacciRays()
    {
        Vector3[] directions = new Vector3[numViewDirections];
        float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < numViewDirections; i++)
        {
            float t = (float)i / numViewDirections;
            float inclination = Mathf.Acos(1 - 2 * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = 0;
            float z = Mathf.Cos(inclination);
            directions[i] = new Vector3(x, y, z);
        }
    }
}
