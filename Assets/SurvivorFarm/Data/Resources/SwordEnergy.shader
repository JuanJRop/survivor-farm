Shader "SurvivorFarm/SwordEnergy"
{
    Properties
    {
        _Tint ("Energy", Color)=(1,.82,.4,1)
        _Age ("Age", Range(0,1))=0
        _Power ("Power", Range(0,1))=0
        _Charge ("Charge mode", Float)=0
        _Direction ("Sweep direction", Float)=1
        _Pulse ("Soft pulse", Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0; };
            fixed4 _Tint;float _Age,_Power,_Charge,_Direction,_Pulse;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                // Quantised world-space artwork, not a smooth debug range circle.
                float2 p=(floor(i.uv*64)/64-.5)*2;
                float r=length(p),a=atan2(p.y,p.x);
                float angular=frac((a*_Direction+3.141593)/6.283186);
                float ring,core,opacity;
                if(_Charge>.5)
                {
                    float radius=.42+.28*_Power;
                    float band=abs(r-radius);
                    float run=pow(saturate(cos(a*3-_Pulse*2.4)),10);
                    ring=saturate(1-band/.055)*(.22+.5*_Power);
                    core=run*saturate(1-band/.14)*(.25+.6*_Power);
                    float sparks=pow(saturate(cos(a*8+_Pulse*1.7)),24)*saturate(1-abs(r-radius-.14)/.045);
                    opacity=(ring+core+sparks*.55)*(.75+.15*sin(_Pulse*3));
                }
                else
                {
                    float head=frac(_Age*.92+.08);
                    float tail=frac(head-angular);
                    float sweep=saturate(1-tail/(.3+.13*_Power));
                    float radius=.63+.27*_Age;
                    float thickness=(.045+.12*_Power)*sweep;
                    float band=abs(r-radius);
                    core=saturate(1-band/max(.01,thickness));
                    ring=saturate(1-band/(thickness+.1))*.3;
                    float shock=saturate(1-abs(r-_Age*.98)/.035)*_Power*.32;
                    opacity=((core+ring)*sweep+shock)*(1-_Age);
                }
                clip(opacity-.012);
                return fixed4(lerp(_Tint.rgb,float3(1,1,.9),saturate(core)*.72),saturate(opacity)*_Tint.a);
            }
            ENDCG
        }
    }
}
