Shader "NewChromantics/DepthBlit" {
   Properties
     {
         DepthMax ("DepthMax", Range(0,0.3)) = 0.1
     }
     SubShader {
Tags { "RenderType"="Opaque" }

Pass{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"


float DepthMax;
sampler2D _CameraDepthTexture;

struct v2f {
   float4 pos : SV_POSITION;
   float4 scrPos:TEXCOORD1;
};

//Vertex Shader
v2f vert (appdata_base v){
   v2f o;
   o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
   o.scrPos=ComputeScreenPos(o.pos);
   //for some reason, the y position of the depth texture comes out inverted
   //	gr: not inverted
    // o.scrPos.y = 1 - o.scrPos.y;
   return o;
}

//Fragment Shader
half4 frag (v2f i) : COLOR{

  	float depthValue = Linear01Depth (tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.scrPos)).r);
  	float DepthNorm = depthValue / DepthMax;

  	DepthNorm = clamp( 0, 1, DepthNorm );
  	DepthNorm = 1 - DepthNorm;

  	return float4( DepthNorm, DepthNorm, DepthNorm, 1 );
}
ENDCG
}
}
FallBack "Diffuse"
}