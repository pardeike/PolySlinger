Shader "Unlit/Beam"
{
	Properties
	{
		_Color("Color1", Color) = (1,1,1,1)
		_Color2("Color2", Color) = (0,0,0,1)
		_GridSpacing("Grid Spacing", float) = 16
		_DotSpacing("Dot Spacing", float) = 0.1
		_DotFrequency("Dot Frequency", float) = 1
		_RangeStart("Range Star", float) = 6
		_RangeEnd("Range End", float) = 10
	}

		SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off

		Pass {

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float2 depth : TEXCOORD0;
			};

			fixed4 _Color;
			fixed4 _Color2;
			float _GridSpacing;
			float _DotSpacing;
			float _DotFrequency;
			float _RangeStart;
			float _RangeEnd;

			v2f vert(appdata_full v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.worldPos = v.vertex;
				o.uv = o.worldPos.xy / _GridSpacing;
				COMPUTE_EYEDEPTH(o.depth);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float offsetx = _Time * 37;
				float offsety = _Time * 43;

				float2 d = frac(i.uv) * 2.0f - 1.0f;
				float l = _DotSpacing / 10;
				fixed4 c = abs(sin(d.x * _DotFrequency + offsetx)) < l && abs(cos(d.y * _DotFrequency + offsety)) < l ? _Color : _Color2;

				c.a = clamp((i.depth - _RangeStart) / _RangeEnd, 0, 1);

				return c;
			}

			ENDCG
		}
	}
}
