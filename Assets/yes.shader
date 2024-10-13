Shader "Unlit/raytrace"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            struct RayTracingMaterial{
                float4 colour;
                float4 emissionColour;
                float emissionStrength;
                };
            struct Sphere
            {
                float3 position;
                float radius;
                RayTracingMaterial material;
            };

      
            uniform int MaxBounceCount;
            uniform float3 _ViewParams;
            uniform float4x4 _CamLocalToWorldMatrix;
            uniform float4 _Color;
            uniform float NumRaysPerPixel;
            uniform int NumRenderedFrames;

            uniform StructuredBuffer<Sphere> Spheres;
            uniform int NumSpheres;


            struct Ray{
                float3 origin;
                float3 dir;
             };
             struct HitInfo{
                 bool didHit;
                 float dst;
                 float3 hitPoint;
                 float3 normal;
                 RayTracingMaterial material;
             };
             //enviroment
            uniform float4 GroundColour;
			uniform float4 SkyColourHorizon;
			uniform float4 SkyColourZenith;
			uniform float SunFocus;
			uniform float SunIntensity;
             float3 GetEnviromentLight(Ray ray)
             {
                 float skyGradientT = pow(smoothstep(0,0.4,ray.dir.y),0.35);
                 float3 skyGradient = lerp(SkyColourHorizon,SkyColourZenith, skyGradientT);
                 float sun = pow(max(0,dot(ray.dir,-_WorldSpaceLightPos0.xyz)),SunFocus)*SunIntensity;

                 float groundToSkyT = smoothstep(-0.01,0,ray.dir.y);
                 float sunMask = groundToSkyT>=1;
                 return lerp(GroundColour,skyGradient,groundToSkyT)+sun*sunMask;
              }
             float RandomValue(inout uint state)
             {
                state = state*747796405 + 2891336453;
                uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
                result = (result >> 22) ^ result;
                return result/4294967295.0;
             }
             float RandomValueNormalDistribution(inout uint state)
             {
                 float theta = 2 * 3.1415926 * RandomValue(state);
                 float rho = sqrt(-2 * log(RandomValue(state)));
                 return rho * cos(theta);
             }
             float3 RandomDirection(inout uint state){
                 float x = RandomValueNormalDistribution(state);
				float y = RandomValueNormalDistribution(state);
				float z = RandomValueNormalDistribution(state);
				return normalize(float3(x, y, z));
                 }
             float3 RandomHemisphereDirection(float3 normal, inout uint rngState){
                 float3 dir = RandomDirection(rngState);
                 return dir * sign(dot(normal,dir));
                 }
            HitInfo RaySphere(Ray ray, float3 sphereCentre, float sphereRadius){
                HitInfo hitInfo = (HitInfo)0;
                float3 offsetRayOrigin = ray.origin - sphereCentre;
                float a = dot(ray.dir,ray.dir);
                float b = 2 * dot(offsetRayOrigin, ray.dir);
                float c = dot(offsetRayOrigin, offsetRayOrigin) - sphereRadius * sphereRadius;
                float discriminant = b*b-4*a*c;

                if(discriminant >= 0){
                    float dst = (-b-sqrt(discriminant))/(2*a);
                    if(dst >=0){
                        hitInfo.didHit = true;
                        hitInfo.dst = dst;
                        hitInfo.hitPoint = ray.origin+ray.dir*dst;
                        hitInfo.normal=normalize(hitInfo.hitPoint-sphereCentre);
                        }
                }
                return hitInfo;
            }
            HitInfo CalculateRayCollision(Ray ray)
            {
                HitInfo closestHit = (HitInfo)0;
                closestHit.dst = 1.#INF;

                for(int i = 0; i < NumSpheres; i++)
                {
                    Sphere sphere = Spheres[i];
                    HitInfo hitInfo = RaySphere(ray,sphere.position,sphere.radius);

                    if(hitInfo.didHit && hitInfo.dst < closestHit.dst){
                        closestHit = hitInfo;
                        closestHit.material = sphere.material;
                        }
                }
               
                return closestHit;
            }
            
            float3 Trace(Ray ray,inout uint rngState){
                float3 rayColour = 1;
                float3 incomingLight = 0;

                for(int i = 0; i <= MaxBounceCount;i++){
                    HitInfo hitInfo = CalculateRayCollision(ray);
                    if(hitInfo.didHit){
                        ray.origin = hitInfo.hitPoint;
                        ray.dir = RandomHemisphereDirection(hitInfo.normal,rngState);

                        RayTracingMaterial material = hitInfo.material;
                        float3 emittedLight = material.emissionColour * material.emissionStrength;
                        incomingLight += emittedLight*rayColour;
                        rayColour *= material.colour;
                        rayColour = saturate(rayColour);
                        }
                        else{
                            incomingLight += GetEnviromentLight(ray) * rayColour;
                            break;
                            }
                    }
                    return incomingLight;
                }

            

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv *  numPixels;
                uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x;
                uint rngState = pixelIndex + NumRenderedFrames * 719393;

                float3 viewPointLocal = float3(i.uv-0.5,1)*_ViewParams;
                float3 viewPoint = mul(_CamLocalToWorldMatrix,float4(viewPointLocal,1));

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint-ray.origin);

                
                float3 totalIncomingLight = 0;

                for(int i=0; i < NumRaysPerPixel; i++){
                    totalIncomingLight += Trace(ray,rngState);
                    }
                
                float3 pixelCol = totalIncomingLight/NumRaysPerPixel;
                return float4(pixelCol,1);
            }
            ENDCG
        }
    }
}
