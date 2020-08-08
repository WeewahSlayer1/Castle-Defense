Shader "Experimental/AnimatedNoise"
{
	//--------------------  PROPERTIES: visible in inspector  ---------------------------------------------------//
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Noise1("Albedo (RGB)", 2D) = "white" {}
		_Noise2("Albedo (RGB)", 2D) = "white" {}
		_Solid("Solid", Range(0, 1)) = .5
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "ForceNoShadowCasting" = "True"}

			//------------------------  DEPTH PASS  ----------------------//
			Pass {
				ColorMask 0
			}

			CGPROGRAM

			#pragma surface surf Standard alpha:fade		//alpha and alpha:fade don't have much difference

			#pragma target 3.0

			sampler2D _Noise1;
			sampler2D _Noise2;

			uniform float _Solid;

		//input struct which is automatically filled by unity
		struct Input
		{
			float2 uv_Noise1;											//Nothing special
			float2 uv2_Noise2;
		};

		fixed4 _Color;

		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = _Color * tex2D(_Noise2, IN.uv2_Noise2).r;

			o.Albedo = _Color;
			o.Alpha = (tex2D(_Noise1, IN.uv_Noise1) * (1 - _Solid) + _Solid) * _Color.a * c.a;
		}
		ENDCG
		}
			FallBack "Diffuse"
}
