Shader "ProjectMystery/DrawnStreetAtmosphere"
{
    Properties { _Tint("Tint",Color)=(1,1,1,.1) _Mode("Mode",Float)=0 }
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
            struct vIn { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct vOut { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0; };
            float4 _Tint;float _Mode;
            vOut vert(vIn v) { vOut o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            fixed4 frag(vOut i):SV_Target
            {
                float2 uv=i.uv;float alpha=0;
                if(_Mode<.5)
                {
                    float width=lerp(.40,.015,uv.y);
                    alpha=(1-smoothstep(width*.35,width,abs(uv.x-.5)))*smoothstep(0,.14,uv.y)*(1-smoothstep(.97,1,uv.y));
                    alpha*=.93+.025*sin(_Time.y*.63);
                }
                else if(_Mode<1.5)
                {
                    float y=uv.y+.08*sin(uv.x*27+_Time.y*.09)+.04*sin(uv.x*63-_Time.y*.13);
                    alpha=exp(-pow((y-.5)*3.7,2))*(.66+.12*sin(uv.x*41+_Time.y*.07));
                    alpha*=smoothstep(0,.06,uv.x)*(1-smoothstep(.94,1,uv.x));
                }
                else if(_Mode<2.5)
                {
                    float2 q=(uv-.5)*2;alpha=pow(saturate(1-length(q)),2);
                }
                else
                {
                    float2 p=uv*float2(200,13);p.x+=p.y*.07;p.y+=_Time.y*4.6;
                    float2 cell=floor(p);float2 local=frac(p);float rnd=hash(cell);
                    float x=abs(local.x-(.18+.64*hash(float2(cell.x,1))));
                    float thin=1-smoothstep(.012,.032,x);
                    alpha=thin*smoothstep(.1,.2,local.y)*(1-smoothstep(.40,.65,local.y))*step(.35,rnd);
                }
                return fixed4(_Tint.rgb,saturate(alpha)*_Tint.a);
            }
            ENDCG
        }
    }
}
