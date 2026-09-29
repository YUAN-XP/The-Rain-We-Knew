Shader "ProjectMystery/EnvironmentEdgeBlend"
{
 Properties { [PerRendererData] _MainTex("Paint",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Edge("Horizontal blend",Range(0,.25))=.06 }
 SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
 Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
 struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
 sampler2D _MainTex; float4 _Color; float _Edge;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 fixed4 frag(v2f i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv)*i.color;c.a*=smoothstep(0,_Edge,i.uv.x)*smoothstep(0,_Edge,1-i.uv.x);return c;}
 ENDCG
 }
 }
}
