Shader "ProjectMystery/P05FeatheredPainting"
{
 Properties{_MainTex("Painting",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _Region("Region",Vector)=(0,0,1,1) _Repeat("Repeat",Float)=1 _RepeatY("Native vertical repeat",Float)=1 _FadeEdge("Right feather",Vector)=(38,40.5,0,0)}
 SubShader{
  Tags{"Queue"="Transparent" "RenderType"="Transparent"}Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass{
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float worldX:TEXCOORD1;};
   sampler2D _MainTex;float4 _Tint,_Region,_FadeEdge;float _Repeat,_RepeatY;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.worldX=mul(unity_ObjectToWorld,v.vertex).x;return o;}
   fixed4 frag(v2f i):SV_Target{
    float x=1-abs(frac(i.uv.x*_Repeat*.5)*2-1);
    float y=1-abs(frac((1-(1-i.uv.y)*_RepeatY)*.5)*2-1);
    fixed4 c=tex2D(_MainTex,_Region.xy+float2(x,y)*_Region.zw)*_Tint; c.a*=1-smoothstep(_FadeEdge.x,_FadeEdge.y,i.worldX);return c;
   }
   ENDCG
  }
 }
}
