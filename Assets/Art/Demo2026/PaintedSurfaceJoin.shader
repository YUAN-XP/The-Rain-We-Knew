Shader "ProjectMystery/PaintedSurfaceJoin"
{
 Properties
 {
  [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
  _Color ("Tint", Color) = (1,1,1,1)
  _Region ("Atlas region", Vector) = (0,0,1,1)
  _JoinWidth ("Edge blend fraction", Range(.01,.25)) = .10
  [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
  [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
  [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
  [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
  Cull Off Lighting Off ZWrite Off
  Blend One OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex SpriteVert
   #pragma fragment Fragment
   #pragma multi_compile_instancing
   #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
   #include "UnitySprites.cginc"
   float4 _Region;
   float _JoinWidth;
   fixed4 Fragment(v2f i) : SV_Target
   {
    // Opposite edge samples converge to exactly the same colour at either seam.
    // The middle 80% remains the original painting; geometry and pixel density stay unchanged.
    float x=saturate((i.texcoord.x-_Region.x)/_Region.z);
    float2 partner=float2(_Region.x+(1-x)*_Region.z,i.texcoord.y);
    float weight=.5*(1-smoothstep(0,_JoinWidth,min(x,1-x)));
    fixed4 c=lerp(SampleSpriteTexture(i.texcoord),SampleSpriteTexture(partner),weight)*i.color;
    c.rgb*=c.a;
    return c;
   }
   ENDCG
  }
 }
}
