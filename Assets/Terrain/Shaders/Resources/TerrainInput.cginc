struct TerrainVertexOutputDeferred
{
    float4 pos : SV_POSITION;
    float4 tex : TEXCOORD0;
    float3 eyeVec : TEXCOORD1;
    float4 tangentToWorldAndPackedData[3] : TEXCOORD2; // [3x3:tangentToWorld | 1x3:viewDirForParallax or worldPos]
    half4 ambientOrLightmapUV : TEXCOORD5; // SH or Lightmap UVs
    
    #if UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT
        float3 posWorld                     : TEXCOORD6;
    #endif

                  
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

StructuredBuffer<float4> _PositionBuffer;
StructuredBuffer<float2> _HeightmapAtlasPosBuffer;
StructuredBuffer<float2> _NormalmapAtlasPosBuffer;

int _HeightmapSize;
int _NormalmapSize;
int _HeightmapSizePadded;
int _NormalmapSizePadded;

float _HeightScale;

UNITY_DECLARE_TEX2D(_HeightmapAtlas);
UNITY_DECLARE_TEX2D(_NormalmapAtlas);

UNITY_DECLARE_TEX2DARRAY(_TerrainAlbedo);
UNITY_DECLARE_TEX2DARRAY(_TerrainNormal);


void terrain_instancing_setup()
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED			
	//_Color = _ColorBuffer[unity_InstanceID];

	float4 data = _PositionBuffer[unity_InstanceID];

	unity_ObjectToWorld._11_21_31_41 = float4(data.w, 0, 0, 0);
	unity_ObjectToWorld._12_22_32_42 = float4(0, 1, 0, 0);
	unity_ObjectToWorld._13_23_33_43 = float4(0, 0, data.w, 0);
	unity_ObjectToWorld._14_24_34_44 = float4(data.x, 0, data.z, 1);
	unity_WorldToObject = unity_ObjectToWorld;
	unity_WorldToObject._14_24_34 *= -1;
	unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
#endif
}
