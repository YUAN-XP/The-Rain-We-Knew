Shader "ProjectMystery/P03FragmentPainting"
{
 Properties
 {
  [PerRendererData] _MainTex ("Fragment",2D)="white"{}
  _Color ("Tint",Color)=(1,1,1,1)
  _Region ("Native sprite region",Vector)=(0,0,1,1)
  _EdgeWidth ("Composite edge blend",Range(0.001,0.15))=0.085
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
  [PerRendererData] _AlphaTex("External Alpha",2D)="white"{}
  [PerRendererData] _EnableExternalAlpha("External Alpha Enabled",Float)=0
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex SpriteVert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
   #include "UnitySprites.cginc"
   float4 _Region;float _EdgeWidth;
   fixed4 frag(v2f i):SV_Target
   {
    float2 uv=(i.texcoord-_Region.xy)/_Region.zw;
    float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
    float irregular=.012+.007*sin(uv.x*43)+.005*sin(uv.y*59);
    fixed4 c=SampleSpriteTexture(i.texcoord)*i.color;
    c.a*=smoothstep(0,_EdgeWidth,edge-irregular);c.rgb*=c.a;return c;
   }
   ENDCG
  }
 }
}