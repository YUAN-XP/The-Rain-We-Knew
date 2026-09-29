Shader "ProjectMystery/SoftForeground"
{
 Properties {
  [PerRendererData] _MainTex("Silhouette",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  _Softness("Edge softness in texture pixels",Float)=4
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex SpriteVert
   #pragma fragment frag
   #include "UnitySprites.cginc"
   float4 _MainTex_TexelSize;float _Softness;
   fixed4 frag(v2f i):SV_Target {
    float2 d=_MainTex_TexelSize.xy*_Softness;
    float a=SampleSpriteTexture(i.texcoord).a*4;
    a+=SampleSpriteTexture(i.texcoord+d).a+SampleSpriteTexture(i.texcoord-d).a;
    a+=SampleSpriteTexture(i.texcoord+float2(d.x,-d.y)).a+SampleSpriteTexture(i.texcoord+float2(-d.x,d.y)).a;
    a+=SampleSpriteTexture(i.texcoord+float2(d.x,0)).a+SampleSpriteTexture(i.texcoord-float2(d.x,0)).a;
    a+=SampleSpriteTexture(i.texcoord+float2(0,d.y)).a+SampleSpriteTexture(i.texcoord-float2(0,d.y)).a;
    a=a/12*i.color.a;
    return fixed4(i.color.rgb*a,a);
   }
   ENDCG
  }
 }
}
