// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Experimental/DiffuseCutout" {
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_AlphaTex("Alpha (A)", 2D) = "white" {}

		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5

		_ColorBp("Blueprint Color", Color) = (0.7, 0.4, 0.25, 1)
		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Outline("Outline width", Range(0, 1)) = .1
		_Mixer("Mixer", Range(0, 1)) = .5
	}

		CGINCLUDE
		#include "UnityCG.cginc"

		//////////////////////////////////////////////
		struct appdata								//
		{											//
			float4 vertex : POSITION;				//
			float3 normal : NORMAL;					//
		};											//
													//
		struct v2f									//
		{											//
			float4 pos : POSITION;					//
			float4 color : COLOR;					//
		};											//
		//////////////////////////////////////////////

		//////////////////////////////////////
		struct Input {						//
			float2 uv_MainTex;				//
			float2 uv2_AlphaTex;			//
		};									//
		//////////////////////////////////////

		// These values can be modified via C# scripting
		//////////////////////////////////////
		uniform float _Outline;				//
		uniform float4 _OutlineColor;		//
											//
		uniform float _Mixer;				//
		//////////////////////////////////////


		//Vertex shader
		//////////////////////////////////////////////////////////////////////////////////////////////
		v2f vert(appdata v)																			//
		{																							//
			// just make a copy of incoming vertex data but scaled according to normal direction	//
			v2f o;																					//
			o.pos = UnityObjectToClipPos(v.vertex);													//
																									//
			float3 norm = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);								//
			float2 offset = TransformViewToProjection(norm.xy);										//
																									//
			o.pos.xy += offset * o.pos.z * _Outline * _Mixer;										//
			o.color = _OutlineColor;																//
			o.color.a = _Mixer;
			o.color;
			return o;																				//
		}																							//
		//////////////////////////////////////////////////////////////////////////////////////////////
		ENDCG


		SubShader{
			Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}
			Cull Back
			LOD 200

			CGPROGRAM
			#pragma surface surf Lambert alphatest:_Cutoff addshadow 

			sampler2D	_MainTex;
			fixed4		_Color;
			fixed4		_ColorBp;
			sampler2D	_AlphaTex;

			void surf(Input IN, inout SurfaceOutput o) {
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex).rgba;

				if (c.a < .1f)
					clip(-1);

				c.rgb = c.rgb * _Color * (1 - _Mixer) + _ColorBp * _Mixer;
				float a = tex2D(_AlphaTex, IN.uv2_AlphaTex).a;

				o.Albedo = c.rgb;
				o.Alpha = a;
			}
			ENDCG

			//////////////////////////////////////////////////////////////////
			Pass
			{
				Name "OUTLINE"
				Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}	//
				Cull Front	
				ZWrite On
				ColorMask RGB
				Blend SrcAlpha OneMinusSrcAlpha

				//////////////////////////////////////////////////////////////
				CGPROGRAM													//
				#pragma alphatest:_Cutoff
				#pragma vertex vert											//
				#pragma fragment frag										//
				half4 frag(v2f i) :COLOR { return i.color; }				//
				ENDCG														//
			}																//
			//////////////////////////////////////////////////////////////////
		}

			Fallback "Diffuse"
}