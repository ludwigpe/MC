Shader "Custom/proceduralNL" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass 
		{
			Cull Back


			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag

			struct MyVert
			{
				float3 vertPos : POS1;  
				float3 vertNorm : NORM1;  
			};

			struct myTriangle{
				float3 wsPos1 : POS1;  
				float3 wsNormal1 : NORM1;  
				float3 wsPos2 : POS2;  
				float3 wsNormal2 : NORM2;  
				float3 wsPos3: POS3;  
				float3 wsNormal3 : NORM3;  
			};
			uniform StructuredBuffer<MyVert> vertexBuffer;
			uniform StructuredBuffer<myTriangle> triangleBuffer;
		
		
			struct v2f 
			{
				float4  pos : SV_POSITION;
				float3	normal: NORMAL;
			};

			struct g2f
			{
				float4  pos : SV_POSITION;
				float3	normal: NORMAL;
			};

			/*
			v2f vert(uint id : SV_VertexID)
			{
				v2f OUT;
				MyVert v = vertexBuffer[id];
				OUT.pos = mul(UNITY_MATRIX_VP, float4(v.vertPos.xyz, 1));
				OUT.normal = v.vertNorm;
				return OUT;
			}
			*/
			
			myTriangle vert(uint id : SV_VertexID)
			{
				myTriangle OUT = triangleBuffer[id];
				return OUT;
			}

			[maxvertexcount (3)]
			void geo(inout TriangleStream<g2f> Stream, point myTriangle input[1])
			{
				g2f OUT;
	
				myTriangle t = input[0];			
				OUT.pos = mul(UNITY_MATRIX_VP, float4(t.wsPos1.xyz, 1));
				OUT.normal = t.wsNormal1;
				Stream.Append(OUT);
				
				OUT.pos = mul(UNITY_MATRIX_VP, float4(t.wsPos2.xyz, 1));
				OUT.normal = t.wsNormal2;
				Stream.Append(OUT);
				
				OUT.pos = mul(UNITY_MATRIX_VP, float4(t.wsPos3.xyz, 1));
				OUT.normal = t.wsNormal3;
				Stream.Append(OUT);
				
				Stream.RestartStrip();


			}
			

			float4 frag(v2f IN) : COLOR
			{
				
				return float4(IN.normal, 1.0f);
			}

			ENDCG

		}
	} 
	FallBack "Diffuse"
}
