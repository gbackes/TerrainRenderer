#include "TerrainInput.cginc"

VertexOutputForwardBase TerrainVertForwardBase(VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputForwardBase o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);
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
    o.eyeVec.xyz = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
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

    //We need this for shadow receving
    UNITY_TRANSFER_SHADOW(o, v.uv1);

    o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

    #ifdef _PARALLAXMAP
            TANGENT_SPACE_ROTATION;
            half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
            o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
            o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
            o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
    #endif

    UNITY_TRANSFER_FOG(o, o.pos);
    return o;

}

void TriplanarMapping(VertexOutputForwardBase i, inout FragmentCommonData s)
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

    /*float3x3 tbn = float3x3(
					i.tangentToWorldAndPackedData[0].xyz,
					i.tangentToWorldAndPackedData[1].xyz,
					i.tangentToWorldAndPackedData[2].xyz 
					);

    normal = normalize(mul(tbn, normal));

    s.normalWorld = normal; */ 
}
  

half4 TerrainFragForwardBase(VertexOutputForwardBase i) : SV_Target
{
    FRAGMENT_SETUP(s)
                      
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    #ifndef _DEBUG_MODE
    TriplanarMapping(i, s);
    #else
        float2 pixel = _HeightmapAtlasPosBuffer[unity_InstanceID] + float2(1, 1) + i.tex.xy * (_HeightmapSize - 1);
        float height = _HeightmapAtlas.Load(int3(pixel, 0)).x;
        s.diffColor = float4((float3) height / _HeightScale, 1);

    #endif

    UnityLight mainLight = MainLight();
    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

    half occlusion =  Occlusion(i.tex.xy);
    UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, mainLight);
    

    half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
    
   // UNITY_APPLY_FOG(i.fogCoord, c.rgb);
    return OutputForward(c, s.alpha);
}

