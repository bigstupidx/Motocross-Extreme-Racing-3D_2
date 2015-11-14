Shader "xsickle/Daynight_Trans_Bush_Unlit_Z" {
	Properties {
		_Color ("Color",Color) = (1,1,1,1)
		_Lighting ("Lighting", Range (0, 1) ) = 1
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { 
		"RenderType"="Transparent"
		"Queue" = "Transparent+1"
		 }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Unlit alpha
		fixed4 _Color;
		sampler2D _MainTex;
		float _Translucency;
		float _Lighting;
		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb*_Color.rgb;
			o.Alpha = c.a*_Color.a * _Lighting;
		}
		
		//half4 LightingNoNormals (SurfaceOutput s, half3 dir, half atten) {
		//  	half4 c =  _LightColor0 * ( atten * 2);
		//    c.rgb *= s.Albedo;
		//    c.a = s.Alpha;
		//    return c;
		//}
		
		half4 LightingUnlit (SurfaceOutput s, half3 lightDir, half atten) {
			half4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}		
		
		ENDCG
	} 
	FallBack "Transparent/VertexLit"
}