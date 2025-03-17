Shader "Unlit/123"
{
   Properties
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1) // Color property
        _CullMode ("Cull Mode", Int) = 0          // 0: Off (Both Sides), 1: Front, 2: Back
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" } // Renders after AR plane
        ZWrite Off      // Disable depth writing
        Blend SrcAlpha OneMinusSrcAlpha // Transparency support
        Cull [_CullMode] // Control render face (Front, Back, Both)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color; // Return the color from the property
            }
            ENDCG
        }
    }
}
