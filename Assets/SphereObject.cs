using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereObject : MonoBehaviour
{
    [System.Serializable]
    public struct RayTracingMaterial
    {
        Vector4 colour;
        Vector4 emissionColour;
        float emissionStrength;
        public RayTracingMaterial(Vector4 colour, Vector4 emissionColour, float emissionStrength) { this.colour = colour; this.emissionColour = emissionColour; this.emissionStrength = emissionStrength; }
    };
    public Vector3 position;
    public float radius;
    public Color materialColour;
    public Color emissionColour;
    public float emissionStrength;
}
