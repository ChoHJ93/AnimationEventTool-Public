
Shader"CHJ_Custom/PrimitiveNoZTest"
{
  SubShader
  {
    Tags { "Queue"="Overlay" "RenderType"="Transparent" "DisableBatching"="true" }
    LOD 100

    Pass
    {
      Cull Off
      Blend SrcAlpha OneMinusSrcAlpha
      ZWrite Off ZTest Always

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
