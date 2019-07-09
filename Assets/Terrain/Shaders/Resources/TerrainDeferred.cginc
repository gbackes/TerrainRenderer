#include "TerrainInput.cginc"
            
TerrainVertexOutputDeferred TerrainVertDeferred(VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    TerrainVertexOutputDeferred o;
    UNITY_INITIALIZE_OUTPUT(TerrainVertexOutputDeferred, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    #ifndef UNITY_PROCEDURAL_INSTANCING_ENABLED
    uint unity_InstanceID = 0;
	#endif     

    #ifndef _DEBUG_MODE	
    float2 uv = clamp(v.vertex.xz, 0, 1);
    float2 pixel = _HeightmapAtlasPosBuffer[unity_InstanceID] + float2(1, 1) + uv * (_HeightmapSize - 1);
    
    float height = _HeightmapAtlas.Load(int3(pixel, 0)).x;
    v.vertex.xz = clamp(v.vertex.xz, 0, 1);
    v.vertex.y = height;

    pixel = _NormalmapAtlasPosBuffer[unity_InstanceID] + float2(1, 1) + uv * (_NormalmapSize - 1);
    v.normal.xyz = _NormalmapAtlas.Load(int3(pixel, 0)).rgb;
    #endif


    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);  
    #if UNITY_REQUIRE_FRAG_WORLDPOS
        #if UNITY_PACK_WORLDPOS_WITH_TANGENT
            o.tangentToWorldAndPackedData[0].w = posWorld.x;
            o.tangentToWorldAndPackedData[1].w = posWorld.y;
            o.tangentToWorldAndPackedData[2].w = posWorld.z;
        #else
            o.posWorld = posWorld.xyz;
        #endif
    #endif
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = TexCoords(v);
    o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndPackedData[0].xyz = 0;
        o.tangentToWorldAndPackedData[1].xyz = 0;
        o.tangentToWorldAndPackedData[2].xyz = normalWorld;
    #endif    

    o.ambientOrLightmapUV = 0;
    #ifdef LIGHTMAP_ON
            o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #elif UNITY_SHOULD_SAMPLE_SH
            o.ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, o.ambientOrLightmapUV.rgb);
    #endif
    #ifdef DYNAMICLIGHTMAP_ON
            o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    #ifdef _PARALLAXMAP
            TANGENT_SPACE_ROTATION;
            half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
            o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
            o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
            o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
    #endif

    return o;    
}
                

void TriplanarMapping(VertexOutputDeferred i, inout FragmentCommonData s)
{                                                       

    // Blending factor of triplanar mapping
    float3 bf = normalize(abs(s.normalWorld));
    bf /= dot(bf, (float3) 1);

    // Triplanar mapping
    float2 tx = s.posWorld.yz / 100;
    float2 ty = s.posWorld.zx / 100;
    float2 tz = s.posWorld.xy / 100;

    // Base color
    half4 cx = UNITY_SAMPLE_TEX2DARRAY(_TerrainAlbedo, float3(tx, 0)) * bf.x;
    half4 cy = UNITY_SAMPLE_TEX2DARRAY(_TerrainAlbedo, float3(ty, 1)) * bf.y;
    half4 cz = UNITY_SAMPLE_TEX2DARRAY(_TerrainAlbedo, float3(tz, 2)) * bf.z;
    half4 color = (cx + cy + cz);
    s.diffColor = (color.rgb) * _Color;
    s.alpha = 0; //color.a;
                                                                                         
    half4 nx = UNITY_SAMPLE_TEX2DARRAY(_TerrainNormal, float3(tx, 0)) * bf.x;
    half4 ny = UNITY_SAMPLE_TEX2DARRAY(_TerrainNormal, float3(ty, 1)) * bf.y;
    half4 nz = UNITY_SAMPLE_TEX2DARRAY(_TerrainNormal, float3(tz, 2)) * bf.z;
    half3 normal = UnpackNormal(nx + ny + nz);

  /*  float3x3 tbn = float3x3(
					i.tangentToWorldAndPackedData[0].xyz,
					i.tangentToWorldAndPackedData[1].xyz,
					i.tangentToWorldAndPackedData[2].xyz 
					);

    normal = normalize(mul(tbn, normal));   

    s.normalWorld = normal; */
}
           
void TerrainFragDeferred(
    TerrainVertexOutputDeferred i,
    out half4 outGBuffer0 : SV_Target0,
    out half4 outGBuffer1 : SV_Target1,
    out half4 outGBuffer2 : SV_Target2,
    out half4 outEmission : SV_Target3 // RT3: emission (rgb), --unused-- (a)
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
    ,out half4 outShadowMask : SV_Target4       // RT4: shadowmask (rgba)
#endif
)
{
    #if (SHADER_TARGET < 30)
        outGBuffer0 = 1;
        outGBuffer1 = 1;
        outGBuffer2 = 0;
        outEmission = 0;
        #if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
                    outShadowMask = 1;
        #endif
            return;
    #endif

    FRAGMENT_SETUP(s)      
    UNITY_SETUP_INSTANCE_ID(i);

    #ifndef UNITY_PROCEDURAL_INSTANCING_ENABLED
    uint unity_InstanceID = 0;
	#endif

    #ifndef _DEBUG_MODE
     TriplanarMapping(i, s); 
    #else
        float2 pixel = _HeightmapAtlasPosBuffer[unity_InstanceID] + float2(1, 1) + i.tex.xy * (_HeightmapSize - 1);
        float height = _HeightmapAtlas.Load(int3(pixel, 0)).x;
        s.diffColor = float4((float3) height / _HeightScale, 1);

    #endif
    // no analytic lights in this pass
    UnityLight dummyLight = DummyLight();
    half atten = 1;

    // only GI
    half occlusion = Occlusion(i.tex.xy);
#if UNITY_ENABLE_REFLECTION_BUFFERS
    bool sampleReflectionsInDeferred = false;
#else
    bool sampleReflectionsInDeferred = true;
#endif

    UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

    half3 emissiveColor = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;

    #ifdef _EMISSION
            emissiveColor += Emission (i.tex.xy);
    #endif

    #ifndef UNITY_HDR_ON
        emissiveColor.rgb = exp2(-emissiveColor.rgb);
    #endif

    UnityStandardData data;
    data.diffuseColor = s.diffColor;
    data.occlusion = occlusion;
    data.specularColor = s.specColor;
    data.smoothness = s.smoothness;
    data.normalWorld = s.normalWorld;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    // Emissive lighting buffer
    outEmission = half4(emissiveColor, 1);

    // Baked direct lighting occlusion if any
    #if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
            outShadowMask = UnityGetRawBakedOcclusions(i.ambientOrLightmapUV.xy, IN_WORLDPOS(i));
    #endif
}

