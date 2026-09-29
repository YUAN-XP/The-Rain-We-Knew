Shader "ProjectMystery/PaintedWorldSurface"
{
 Properties {
  [PerRendererData] _MainTex("Paint",2D)="white"{}
  _Region("Atlas rectangle",Vector)=(0,0,1,1)
  _WorldPeriod("World repeat size",Float)=12
  _Color("Tint",Color)=(1,1,1,1)
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnitySprites.cginc"
   float4 _Region;float _WorldPeriod;
   struct Output {float4 vertex:SV_POSITION;float2 world:TEXCOORD0;fixed4 color:COLOR;};
   Output vert(appdata_t v){Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xy;o.color=v.color*_Color*_RendererColor;return o;}
   fixed4 paint(float2 uv){return tex2D(_MainTex,_Region.xy+uv*_Region.zw);}
   fixed4 frag(Output i):SV_Target{
    float2 uv=frac(i.world/max(.1,_WorldPeriod));
    float2 blend=.5*(1-smoothstep(0,.12,min(uv,1-uv)));
    fixed4 a=lerp(paint(uv),paint(float2(1-uv.x,uv.y)),blend.x);
    fixed4 b=lerp(paint(float2(uv.x,1-uv.y)),paint(1-uv),blend.x);
    fixed4 c=lerp(a,b,blend.y)*i.color;c.rgb*=c.a;return c;
   }
   ENDCG
  }
 }
}
