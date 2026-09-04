// Flat pastel lighting: vertex colour * (sky-tinted ambient + soft half-lambert sun),
// with exponential-squared fog so distant water melts into the sky.
Shader "Houscaper/Solid"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Tags { "LightMode" = "Always" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Set once per frame from Bootstrap via Shader.SetGlobal*.
            float4 _HsSunDir;
            float4 _HsSunColor;
            float4 _HsSkyColor;
            float4 _HsGroundColor;
            float4 _HsFogColor;
            float  _HsFogDensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                fixed4 color  : COLOR;
                float3 wnrm   : TEXCOORD0;
                float3 wpos   : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.wnrm = UnityObjectToWorldNormal(v.normal);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.wnrm);
                float3 l = normalize(_HsSunDir.xyz);

                // Half-lambert keeps shadowed faces pastel instead of muddy.
                float ndl = saturate(dot(n, l) * 0.5 + 0.5);
                float up  = saturate(n.y * 0.5 + 0.5);

                float3 ambient = lerp(_HsGroundColor.rgb, _HsSkyColor.rgb, up);
                float3 light = ambient + _HsSunColor.rgb * pow(ndl, 1.4);

                float3 col = i.color.rgb * light;

                float d = distance(i.wpos, _WorldSpaceCameraPos) * _HsFogDensity;
                col = lerp(col, _HsFogColor.rgb, saturate(1.0 - exp(-d * d)));

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
