Shader "Custom/procIndex" {
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
			//uniform float3 _wsChunkPosLL;
			//uniform float3 _wsChunkDim;
			struct MyVert
			{
				float3 vertPos : POS1;  
				float3 vertNorm : NORM1;  
			};
			struct myTriangle{
				float3 wsPos1;  
				float3 wsNormal1;  
				float3 wsPos2;  
				float3 wsNormal2;  
				float3 wsPos3;  
				float3 wsNormal3; 
			};
			uniform StructuredBuffer<MyVert> vertexBuffer;
			uniform StructuredBuffer<int> indexBuffer;
			uniform float4x4 modelMat;

			struct v2g{
				int triIndex : tID;
			};
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
				int vIndex = indexBuffer[id];
				MyVert vertex = vertexBuffer[id];
				//OUT.pos = mul(modelMat, float4(vertex.vertPos.xyz, 1));
				OUT.pos = mul(UNITY_MATRIX_VP, float4(vertex.vertPos.xyz, 1));
				//OUT.pos = mul(UNITY_MATRIX_VP, OUT.pos);
				OUT.normal = vertex.vertNorm;
				return OUT;
			}
			*/
			v2g vert(uint id : SV_VertexID)
			{
				v2g OUT;
				OUT.triIndex = id;
				return OUT;
			}
			[maxvertexcount (3)]
			void geo(inout TriangleStream<g2f> Stream, point v2g input[1])
			{
				g2f OUT;
				int startIndex = input[0].triIndex * 3;
				MyVert vertex;
				
				vertex = vertexBuffer[indexBuffer[startIndex]];
				
				OUT.pos = mul(UNITY_MATRIX_VP, float4(vertex.vertPos.xyz, 1));
				OUT.normal = vertex.vertNorm;
				Stream.Append(OUT);
				
				vertex = vertexBuffer[indexBuffer[startIndex+ 1]];
				OUT.pos = mul(UNITY_MATRIX_VP, float4(vertex.vertPos.xyz, 1));
				OUT.normal = vertex.vertNorm;
				Stream.Append(OUT);
				
				vertex = vertexBuffer[indexBuffer[startIndex+ 2]];
				OUT.pos = mul(UNITY_MATRIX_VP, float4(vertex.vertPos.xyz, 1));
				OUT.normal = vertex.vertNorm;
				Stream.Append(OUT);
				
				Stream.RestartStrip();


			}


			float4 frag(g2f IN) : COLOR
			{
				
				return float4(IN.normal, 1.0f);
			}

			ENDCG

		}
	} 
	FallBack "Diffuse"
}
