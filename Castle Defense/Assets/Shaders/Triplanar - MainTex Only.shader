// Standard shader with triplanar mapping
// https://github.com/keijiro/StandardTriplanar

Shader "Experimental/TriplanarMaintex"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("MainTex", 2D) = "white" {}

		_MapScale("_MapScale", Float) = 1
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }

			CGPROGRAM

			#pragma surface surf Standard vertex:vert fullforwardshadows addshadow
			#pragma target 3.0

			half4 _Color;
			sampler2D _MainTex;

			half _MapScale;

			struct Input
			{
				float3 localCoord;
				float3 localNormal;
			};

			void vert(inout appdata_full v, out Input data)
			{
				UNITY_INITIALIZE_OUTPUT(Input, data);
				data.localCoord = v.vertex.xyz;
				data.localNormal = v.normal.xyz;
			}

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				// Blending factor of triplanar mapping
				float3 bf = normalize(abs(IN.localNormal));
				bf /= dot(bf, (float3)1);

				// Triplanar mapping
				float2 tx = IN.localCoord.yz * _MapScale;
				float2 ty = IN.localCoord.zx * _MapScale;
				float2 tz = IN.localCoord.xy * _MapScale;

				// Base color
				half4 cx = tex2D(_MainTex, tx) * bf.x;
				half4 cy = tex2D(_MainTex, ty) * bf.y;
				half4 cz = tex2D(_MainTex, tz) * bf.z;
				half4 color = (cx + cy + cz) * _Color;
				o.Albedo = color.rgb;
				o.Alpha = color.a;
			}
			ENDCG
		}
			FallBack "Diffuse"
				CustomEditor "StandardTriplanarInspector"
}