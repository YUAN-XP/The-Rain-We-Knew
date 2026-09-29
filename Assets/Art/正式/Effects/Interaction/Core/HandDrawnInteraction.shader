Shader "ProjectMystery/HandDrawnInteraction"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LineColor ("Pencil color", Color) = (1,.87,.62,.78)
        _StrokePixels ("Stroke screen pixels", Range(.5,4)) = 1.6
        _RedrawRate ("Redraw frames per second", Range(2,12)) = 6
        _StrokeVariation ("Stroke variation", Range(0,.35)) = .16
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float4 _LineColor;
            float _StrokePixels, _RedrawRate, _StrokeVariation;
            float Mask(float2 uv) { return smoothstep(.18,.42,SampleSpriteTexture(uv).a); }
            fixed4 Frag(v2f input) : SV_Target
            {
                float2 uv = input.texcoord;
                float phase = floor(_Time.y * _RedrawRate);
                float variation = sin(dot(uv, float2(69.7,47.3)) + phase * 1.71);
                float width = _StrokePixels * (1 + _StrokeVariation * variation);
                float2 px = max(fwidth(uv), float2(.000001,.000001)) * width;
                float center = Mask(uv);
                float lo = center, hi = center;
                float a;
                a=Mask(uv+float2(px.x,0)); lo=min(lo,a); hi=max(hi,a);
                a=Mask(uv-float2(px.x,0)); lo=min(lo,a); hi=max(hi,a);
                a=Mask(uv+float2(0,px.y)); lo=min(lo,a); hi=max(hi,a);
                a=Mask(uv-float2(0,px.y)); lo=min(lo,a); hi=max(hi,a);
                a=Mask(uv+px*.7071); lo=min(lo,a); hi=max(hi,a);
                a=Mask(uv-px*.7071); lo=min(lo,a); hi=max(hi,a);
                a=Mask(uv+float2(px.x,-px.y)*.7071); lo=min(lo,a); hi=max(hi,a);
                a=Mask(uv+float2(-px.x,px.y)*.7071); lo=min(lo,a); hi=max(hi,a);
                float edge = saturate((hi-lo)*1.65);
                float pencil = .8 + .2*sin(dot(uv,float2(113,179))+phase*.47);
                return fixed4(_LineColor.rgb, edge * pencil * _LineColor.a * input.color.a);
            }
            ENDCG
        }
    }
}
