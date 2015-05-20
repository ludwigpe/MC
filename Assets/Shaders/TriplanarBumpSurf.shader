Shader "Tri-Planar BumpSurf" {
  Properties {
		_Side("Side", 2D) = "white" {}
		_SideNormal("Side Normalmap", 2D) = "white" {}
		_Top("Top", 2D) = "white" {}
		_TopNormal("Top Normalmap", 2D) = "white" {}
		_Bottom("Bottom", 2D) = "white" {}
		_BottomNormal("Bottom Normalmap", 2D) = "white" {}
		_NormalPower("Normal power", Float) = 1.0
		_Tess ("Tessellation", Range(1,32)) = 4
	}
	
	SubShader {
		Tags {
			"Queue"="Geometry"
			"IgnoreProjector"="False"
			"RenderType"="Opaque"
		}
 
		Cull Back
		ZWrite On
		
		CGPROGRAM
		#pragma surface surf BlinnPhong addshadow fullforwardshadows vertex:vert
       
		//#pragma surface surf Lambert vertex:vert 
		#pragma target 5.0
		#pragma exclude_renderers flash
 
		// user defined variables
		uniform sampler2D _Side, _Top, _Bottom;
		uniform sampler2D _SideNormal, _TopNormal, _BottomNormal;
		uniform float4 _Side_ST, _Top_ST, _Bottom_ST;
		uniform float4 _SideNormal_ST, _TopNormal_ST, _BottomNormal_ST;
		uniform float _NormalPower;
		uniform float _Tess;
		

		struct Input {
			float3 worldPos;
			//float3 worldNormal;
			float3 wNormal;
			//INTERNAL_DATA
		};
			

		void vert (inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input,data);
			data.wNormal = mul(_Object2World, v.normal);
		}

		void surf (Input IN, inout SurfaceOutput o) 
		{
			
			float3 nDir = IN.wNormal;
			//nDir = WorldNormalVector(IN, o.Normal);; 
			float3 wsPos = IN.worldPos;
			float3 blending = abs(nDir);
			blending = normalize(max(blending,0.00001));
			float b = (blending.x + blending.y + blending.z);
			blending /= float3(b,b,b);
			
			// texture and normal sampling
			float4 xaxis = tex2D(_Side, wsPos.yz * _Side_ST.xy + _Side_ST.zw);
			float3 xaxisN = UnpackNormal(tex2D(_SideNormal, wsPos.yz * _SideNormal_ST.xy + _SideNormal_ST.zw));
			
			float4 yaxis;
			float3 yaxisN;
			if(nDir.y > 0)
			{
				yaxis = tex2D(_Top, wsPos.xz * _Top_ST.xy + _Top_ST.zw);
				yaxisN = UnpackNormal(tex2D(_TopNormal, wsPos.xz * _TopNormal_ST.xy + _TopNormal_ST.zw));
			}
			else
			{
				yaxis = tex2D(_Bottom, wsPos.xz * _Bottom_ST.xy + _Bottom_ST.zw);
				yaxisN = UnpackNormal(tex2D(_BottomNormal, wsPos.xz * _BottomNormal_ST.xy + _BottomNormal_ST.zw));
			}
			
			float4 zaxis = tex2D(_Side, wsPos.xy * _Side_ST.xy + _Side_ST.zw);
			float3 zaxisN = UnpackNormal(tex2D(_SideNormal, wsPos.xy * _SideNormal_ST.xy + _SideNormal_ST.zw));
			
			// blend texture and normals by blend weights
			float4 tex = xaxis * blending.x + yaxis * blending.y + zaxis * blending.z;
			float3 texN = xaxisN * blending.x + yaxisN * blending.y + zaxisN * blending.z;
			
			o.Albedo = tex;
			o.Normal = texN;

			nDir = IN.wNormal;
			float3 normalAxisX = UnpackNormal(tex2D(_SideNormal, wsPos.yz * _SideNormal_ST.xy + _SideNormal_ST.zw)).zyx;
			normalAxisX.x *= sign(nDir.x);
			
			float3 normalAxisY;
			if(nDir.y > 0)
			{
				normalAxisY = UnpackNormal(tex2D(_TopNormal, wsPos.xz * _TopNormal_ST.xy + _TopNormal_ST.zw)).xzy;
			}
			else
			{
				normalAxisY = UnpackNormal(tex2D(_BottomNormal, wsPos.xz * _BottomNormal_ST.xy + _BottomNormal_ST.zw)).xzy;
			}
			normalAxisY.y *= sign(nDir.y);

			float3 normalAxisZ = UnpackNormal(tex2D(_SideNormal, wsPos.xy * _SideNormal_ST.xy + _SideNormal_ST.zw)).yxz;
			normalAxisZ.z *= sign(nDir.z);

			float3 finalNormal = normalAxisX * blending.x + normalAxisY * blending.y + normalAxisZ * blending.z;

			//o.Normal = finalNormal;
			o.Specular = 0.5f;
			o.Gloss = 10.0f;
		} 
		ENDCG
	}
	Fallback "Diffuse"
}