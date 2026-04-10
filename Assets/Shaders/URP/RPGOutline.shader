// ────────────────────────────────────────────────────────────────────────────
//  Hidden/RPG/Outline
//
//  Pass 0  RPG_Silhouette
//    用途：由 OutlineRenderPass 以 overrideMaterial 调用，
//          将对象的不透明像素渲染为白色掩膜（写入 R8 临时 RT）。
//    调用方：DrawRenderers → overrideMaterial = 本 Shader，passIndex = 0
//
//  Pass 1  RPG_OutlineComposite
//    用途：全屏三角形 DrawProcedural 叠加至相机颜色 RT。
//          对掩膜纹理做 8 方向膨胀，取边缘环像素 = 轮廓颜色，
//          混合模式 SrcAlpha OneMinusSrcAlpha 实现透明叠加。
//    调用方：DrawProcedural → passIndex = 1
// ────────────────────────────────────────────────────────────────────────────
Shader "Hidden/RPG/Outline"
{
    Properties
    {
        [HideInInspector] _MainTex      ("Sprite Texture",  2D) = "white" {}
        [HideInInspector] _MaskTex      ("Outline Mask",    2D) = "black" {}
                          _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.1, 1)
                          _OutlineWidth ("Outline Width",  Int)  = 2
        [HideInInspector] _MaskTexelSize("Mask Texel Size", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        // ════════════════════════════════════════════════════════════════════
        //  Pass 0 — Silhouette
        //  将精灵/网格的不透明区域渲染为白色，写入 R8 掩膜 RT。
        //  Alpha 测试剔除透明像素（精灵边缘）。
        // ════════════════════════════════════════════════════════════════════
        Pass
        {
            Name "RPG_Silhouette"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZTest   Always
            ZWrite  Off
            Cull    Off
            Blend   Off

            HLSLPROGRAM
            #pragma vertex   SilhouetteVert
            #pragma fragment SilhouetteFrag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SilhouetteVert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv         = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            half4 SilhouetteFrag(Varyings i) : SV_Target
            {
                // 采样 Alpha 通道，透明像素丢弃（精灵边缘不留白色）
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                clip(alpha - 0.1h);
                return half4(1, 1, 1, 1); // 输出纯白色到 R8 掩膜
            }
            ENDHLSL
        }

        // ════════════════════════════════════════════════════════════════════
        //  Pass 1 — OutlineComposite
        //  全屏后处理，读取掩膜纹理 → 8 方向膨胀差分 → 叠加轮廓颜色。
        // ════════════════════════════════════════════════════════════════════
        Pass
        {
            Name "RPG_OutlineComposite"

            ZTest  Always
            ZWrite Off
            Cull   Off
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   OutlineVert
            #pragma fragment OutlineFrag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _OutlineColor;
                float  _OutlineWidth;
                float4 _MaskTexelSize; // (1/w, 1/h, w, h)
            CBUFFER_END

            // ── 全屏三角形顶点着色器 ──────────────────────────────────────
            // 通过 DrawProcedural(3 顶点) 生成全屏覆盖三角形，无需 Mesh。

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // 全屏三角形标准公式
                o.texcoord.x = (vertexID << 1) & 2u;
                o.texcoord.y =  vertexID       & 2u;
                o.positionCS = float4(o.texcoord * 2.0 - 1.0, 0, 1);

                // D3D 等平台 Y 轴朝上修正
                #if UNITY_UV_STARTS_AT_TOP
                o.texcoord.y = 1.0 - o.texcoord.y;
                #endif

                return o;
            }

            // ── 膨胀差分片元着色器 ────────────────────────────────────────

            half4 OutlineFrag(Varyings i) : SV_Target
            {
                float2 uv    = i.texcoord;
                float2 texel = _MaskTexelSize.xy * _OutlineWidth;

                // 当前像素是否在掩膜中
                float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uv).r;

                // 8 方向邻居中的最大掩膜值（膨胀核）
                float maxNeighbor = 0;
                UNITY_UNROLL
                for (int x = -1; x <= 1; x++)
                UNITY_UNROLL
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    maxNeighbor = max(maxNeighbor,
                        SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex,
                            uv + float2(x, y) * texel).r);
                }

                // 边缘 = 邻居有掩膜 但当前像素无掩膜（差分 = 轮廓环）
                float outline = saturate(maxNeighbor - mask);

                // 输出轮廓颜色（Alpha = 轮廓强度 × 轮廓透明度设置）
                return half4(_OutlineColor.rgb, outline * _OutlineColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
