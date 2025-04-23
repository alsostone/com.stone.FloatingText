Shader "DMII/Damage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON DEBUG_DISPLAY
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"
            
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vid : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            struct FloatingDamage
            {
                uint3x3 uvVexIdx;
                float2 scale;
                float3 wpos;
                float fixedTime;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            StructuredBuffer<FloatingDamage> _InstanceBuffer;
            StructuredBuffer<uint> _VisibleBuffer;
            StructuredBuffer<float2> _TextUvs;
            StructuredBuffer<float2> _TextVets;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            void setup()
            {
				float4x4 mat = unity_ObjectToWorld;
            	uint id = _VisibleBuffer[unity_InstanceID];
                float3 wpos = _InstanceBuffer[id].wpos;
				mat[0][3] = wpos.x;
				mat[1][3] = wpos.y;
				mat[2][3] = wpos.z;
				unity_ObjectToWorld = mat;
            }
            #endif

            uint UnpackUint4(uint value, uint idx)
            {
                uint a = (value >> (idx * 8)) & 255;
                return a;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float4 vert = v.vertex;
                
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                
            	uint id = _VisibleBuffer[unity_InstanceID];
                uint3x3 idx3x3 = _InstanceBuffer[id].uvVexIdx;
                uint adv = v.vid / 4;
                uint ix = adv % 3;
                int iy = v.vid / 12;
                uint idx4i = 3 - v.vid % 4;
                uint idx4 = idx3x3[iy][ix];
                uint idx = UnpackUint4(idx4, idx4i);
                
                float2 scale = _InstanceBuffer[id].scale;
                v.uv = _TextUvs[idx];
                vert.xy = (float2(0.45 * adv, 0) + _TextVets[idx]) * scale;
                
                #endif

                o.vertex = UnityObjectToClipPos(vert);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
