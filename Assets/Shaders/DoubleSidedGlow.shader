// プレイヤーを包み込むバリアなど、カメラが内側に位置する半透明球のための専用シェーダー。
// 標準のStandardシェーダーは裏面カリング(Cull Back)が固定されており、内側から見ると
// 消えてしまう(実機で発生した不具合)。Cull Offで両面描画し、XREALのシースルーAR表示でも
// 見えるよう単純な加算寄りの発光カラーをそのまま出力する。
Shader "MagicalGirl/DoubleSidedGlow"
{
    Properties
    {
        _Color ("Color", Color) = (0.4, 0.8, 1, 0.6)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
