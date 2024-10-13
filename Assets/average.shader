Shader "Hidden/Accumulate"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

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

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _CurrRender;
			sampler2D _PrevRender;
			int NumRenderedFrames;

			float4 frag (v2f i) : SV_Target
			{
				float4 col = tex2D(_CurrRender, i.uv);
				float4 oldRender = tex2D(_PrevRender, i.uv);
				// Combine prev frame with current frame. Weight the contributions to result in an average over all frames.
				float4 averagedColor = ((oldRender*NumRenderedFrames)+col)/(NumRenderedFrames+1);
				
				return float4(averagedColor.xyz,1);

			}
			ENDCG
		}
	}
}