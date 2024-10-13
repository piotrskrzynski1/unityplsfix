using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RayTracingManager;

public class SphereManager : MonoBehaviour
{

    private ComputeBuffer data;
    private List<Sphere> sphereData = new List<Sphere>();
    public Material raytracingMaterial;
    public struct RayTracingMaterial
    {
        Vector4 colour;
        Vector4 emissionColour;
        float emissionStrength;
        public RayTracingMaterial(Vector4 colour, Vector4 emissionColour, float emissionStrength) { this.colour = colour; this.emissionColour = emissionColour; this.emissionStrength = emissionStrength; }
    };
    public struct Sphere
    {
        public Vector3 position;
        public float radius;
        public RayTracingMaterial material;
        public Sphere(Vector3 position, float radius, RayTracingMaterial RTM) { this.position = position; this.radius = radius; this.material = RTM; }
    };

    // Update is called once per frame
    void Update()
    {
        sphereData.Clear();
        SphereObject[] sphereObjects = this.gameObject.GetComponentsInChildren<SphereObject>();

        foreach (SphereObject sphereObject in sphereObjects)
        {
            RayTracingMaterial thisRTM = new RayTracingMaterial(sphereObject.materialColour, sphereObject.emissionColour, sphereObject.emissionStrength);
            Sphere thisSO = new Sphere(sphereObject.position,sphereObject.radius,thisRTM);
            sphereData.Add(thisSO);
        }

        data = new ComputeBuffer(sphereData.Count, sizeof(float) * 8 + sizeof(float) * 5);
        data.SetData(sphereData);
        raytracingMaterial.SetBuffer("Spheres", data);
        raytracingMaterial.SetFloat("NumSpheres", sphereData.Count);
       
    }

    public void OnDestroy()
    {
        data.Release();
    }
}
