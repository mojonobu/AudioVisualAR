Shader "Custom/URPOverlapShader" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // URPデフォルトのパス
        Pass {
            // ここにURPのデフォルトシェーダーのコードを書きます。
            // 通常のライティングやテクスチャリングなど。
        }

        // カスタムのステンシルパス
        Pass {
            Stencil {
                Ref 1
                Comp equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityURP.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // ステンシルがマッチしたピクセルに対して特定の色で塗りつぶします。
                return _Color;
            }
            ENDCG
        }
    }
    FallBack "Universal Forward"
}
