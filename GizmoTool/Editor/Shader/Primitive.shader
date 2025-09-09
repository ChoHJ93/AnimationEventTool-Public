
Shader "CHJ_Custom/Primitive"
{
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="true" }
    LOD 100

    Pass
    {
      Cull Off
      Blend SrcAlpha OneMinusSrcAlpha

      CGPROGRAM
      #pragma shader_feature NORMAL_ON
      #pragma shader_feature CAP_SHIFT_SCALE
      #pragma vertex vert
      #pragma fragment frag
      #include "PrimitiveCore.cginc"
      ENDCG
    }
  }
}
