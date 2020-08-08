Shader "Experimental/FresnelTransparency"
{
	//--------------------  PROPERTIES: visible in inspector  ---------------------------------------------------//
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5	
		_Metallic("Metallic", Range(0,1)) = 0.0	
		
		[HDR] _Emission("Emission", color) = (0,0,0)								//Nothing special

		_FresnelColor("Fresnel Color", Color) = (1,1,1,1)							//float3 declared line 41
		[PowerSlider(4)] _FresnelExponent("Fresnel Exponent", Range(0.25, 4)) = 1	//float declared line 42
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }



			//------------------------  DEPTH PASS  ----------------------//
			Pass {
				ColorMask 0
			}

			CGPROGRAM	

			#pragma surface surf Standard fullforwardshadows alpha:fade		//alpha and alpha:fade don't have much difference
			#pragma target 3.0

			sampler2D _MainTex;

			//input struct which is automatically filled by unity
			struct Input
			{
				float2 uv_MainTex;											//Nothing special
				float3 worldNormal;											//UNIQUE, used to calculate fresnel
				float3 viewDir;												//UNIQUE
				INTERNAL_DATA												//UNIQUE
			};

			half _Glossiness;
			half _Metallic;
			half3 _Emission;														//Nothing special
			fixed4 _Color;

			float3 _FresnelColor;											//UNIQUE
			float _FresnelExponent;											//UNIQUE

			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_INSTANCING_BUFFER_END(Props)

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				//o.Alpha = c.a;

				float fresnel = dot(IN.worldNormal, IN.viewDir);			//UNIQUE, calculate fresnel
				fresnel = saturate(1 - fresnel);							//UNIQUE, invert the fresnel so the big values are on the outside
				fresnel = pow(fresnel, _FresnelExponent);					//UNIQUE, fresnel exponent

				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo = c.rgb;
				o.Alpha = (1 - c.a) * fresnel + 1 * c.a;

				float3 fresnelColor = fresnel * _FresnelColor;
				o.Emission = ((_Emission + fresnelColor) / _FresnelExponent) * (1 - c.a);
			}
			ENDCG
		}
			FallBack "Diffuse"
}
