Shader "ProjectMystery/PaintedContactShadow"
{
 Properties { _Color("Ink",Color)=(.045,.045,.055,.25) }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct V{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct F{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
   float4 _Color;
   F vert(V v){F f;f.vertex=UnityObjectToClipPos(v.vertex);f.uv=v.uv;return f;}
   fixed4 frag(F f):SV_Target{
    float2 p=f.uv*2-1;
    float radius=dot(p,p);
    float ink=(1-smoothstep(.15,1,radius));
    ink*=.92+.08*sin(p.x*39+p.y*7);
    return fixed4(_Color.rgb,_Color.a*ink);
   }
   ENDCG
  }
 }
}
