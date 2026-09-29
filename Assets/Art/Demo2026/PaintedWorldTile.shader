Shader "ProjectMystery/PaintedWorldTile" {
 Properties {
  [PerRendererData] _MainTex("Paint",2D)="white"{}
  _Period("Metres per native tile",Vector)=(6,4.5,0,0)
  _Color("Tint",Color)=(1,1,1,1)
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass { CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnitySprites.cginc"
   float4 _Period;
   struct Out {float4 vertex:SV_POSITION;float2 world:TEXCOORD0;fixed4 color:COLOR;};
   Out vert(appdata_t v){Out o;o.vertex=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xy;o.color=v.color*_Color*_RendererColor;return o;}
   fixed4 frag(Out i):SV_Target{
    // Mirror addressing keeps edge samples continuous, without alpha dissolves.
    float2 uv=1-abs(frac(i.world/max(float2(.1,.1),_Period.xy)*.5)*2-1);
    fixed4 c=tex2D(_MainTex,uv)*i.color;c.rgb*=c.a;return c;
   }
   ENDCG }
 }
}
