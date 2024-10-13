using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


public class RayTracingManager : MonoBehaviour
{


    public int maxBounceRay = 1;
    public int numRaysPerPixel = 1;
    public int numRenderedFrames = 1;
    public float weight = 0.4f;

    public Vector4 GroundColour;
    public Vector4 SkyColourHorizon;
    public Vector4 SkyColourZenith;
    public float SunFocus;
    public float SunIntensity;

    private void Start()
    {
        UpdateCameraParams(Camera.main);
       
    }
    private void Update()
    {
        ++numRenderedFrames;
 
        UpdateCameraParams(Camera.main);
      
    }

    public bool useShaderInSceneView;
    public Material rayTracingMaterial;
    public Material averageMaterial;
    
    void UpdateCameraParams(Camera cam)
    {
        float planeHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
        float planeWidth = planeHeight * cam.aspect;

        rayTracingMaterial.SetVector("_ViewParams", new Vector3(planeWidth, planeHeight, cam.nearClipPlane));
        rayTracingMaterial.SetMatrix("_CamLocalToWorldMatrix", cam.transform.localToWorldMatrix);
        rayTracingMaterial.SetVector("_Color", new Vector4(0f, 1f, 0f, 1f));
        rayTracingMaterial.SetInt("MaxBounceCount", maxBounceRay);
        rayTracingMaterial.SetInt("NumRaysPerPixel", numRaysPerPixel);
        averageMaterial.SetFloat("weight", weight);
        rayTracingMaterial.SetVector("GroundColour", GroundColour);
        rayTracingMaterial.SetVector("SkyColourHorizon", SkyColourHorizon);
        rayTracingMaterial.SetVector("SkyColourZenith", SkyColourZenith);
        rayTracingMaterial.SetFloat("SunFocus", SunFocus);
        rayTracingMaterial.SetFloat("SunIntensity", SunIntensity);
    
    }
}
