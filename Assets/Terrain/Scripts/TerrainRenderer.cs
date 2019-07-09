using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain
{
	public class TerrainRenderer
	{
		private Terrain m_Terrain;

		public Mesh m_InstanceGridMesh;

		private Vector3[] vertices;
		private Vector3[] normals;
		private Color[] colors;

		private Vector2[] heightmapAtlasPos;
		private Vector2[] normalmapAtlasPos;
		private Vector4[] positions;

		private MaterialPropertyBlock mpb;
		private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };


		private ComputeBuffer m_PositionBuffer;
		private ComputeBuffer m_ArgsBuffer;
		private ComputeBuffer m_HeightmapAtlasPosBuffer;
		private ComputeBuffer m_NormalmapAtlasPosBuffer;

		private Texture2DArray albedoTexture2DArray;
		private Texture2DArray normalTexture2DArray;

		public TerrainRenderer(Terrain terrain)
		{
			m_Terrain = terrain;

			mpb = new MaterialPropertyBlock();

			m_InstanceGridMesh = CreateGridMesh(Terrain.GridSize);

			positions = new Vector4[Terrain.MaxSelectionCount];
			heightmapAtlasPos = new Vector2[Terrain.MaxSelectionCount];
			normalmapAtlasPos = new Vector2[Terrain.MaxSelectionCount];

			CreateTexturesArrays();
			SetComputeBuffers();
			SetUniforms();
		}


		public void Render(Camera cam, List<QuadTreeNode> selection)
		{
			if (selection == null || selection.Count == 0)
				return;

			UpdateBuffers(selection);

			Graphics.DrawMeshInstancedIndirect(m_InstanceGridMesh, 0, m_Terrain.instanceMaterial, new Bounds(Vector3.zero, Vector3.one * 64000f), m_ArgsBuffer, 0, mpb, ShadowCastingMode.On, true, 0, cam, LightProbeUsage.BlendProbes);

		}

		void CreateTexturesArrays()
		{
			CreateTextureArray(ref albedoTexture2DArray, m_Terrain.albedoTextures);
			CreateTextureArray(ref normalTexture2DArray, m_Terrain.normalTextures);
		}

		void CreateTextureArray(ref Texture2DArray textureArray, Texture2D[] textures)
		{
			textureArray = new
			   Texture2DArray(Terrain.TextureArrayRes, Terrain.TextureArrayRes, textures.Length,
			   TextureFormat.RGB24, true, false);
			// Apply settings
			textureArray.filterMode = FilterMode.Bilinear;
			textureArray.wrapMode = TextureWrapMode.Repeat;
			// Loop through ordinary textures and copy pixels to the
			// Texture2DArray
			for (int i = 0; i < textures.Length; i++)
			{
				textureArray.SetPixels(textures[i].GetPixels(0),
					i, 0);
			}
			// Apply our changes
			textureArray.Apply();
		}

		void SetComputeBuffers()
		{
			m_ArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			m_PositionBuffer = new ComputeBuffer(Terrain.MaxSelectionCount, 16);
			m_HeightmapAtlasPosBuffer = new ComputeBuffer(Terrain.MaxSelectionCount, 8);
			m_NormalmapAtlasPosBuffer = new ComputeBuffer(Terrain.MaxSelectionCount, 8);
		}

		void SetUniforms()
		{
			mpb.SetTexture("_HeightmapAtlas", m_Terrain.m_HeightmapAtlas.texture);
			mpb.SetTexture("_NormalmapAtlas", m_Terrain.m_NormalmapAtlas.texture);

			mpb.SetBuffer("_HeightmapAtlasPosBuffer", m_HeightmapAtlasPosBuffer);
			mpb.SetBuffer("_NormalmapAtlasPosBuffer", m_NormalmapAtlasPosBuffer);

			mpb.SetBuffer("_PositionBuffer", m_PositionBuffer);

			Shader.SetGlobalInt("_HeightmapSize", Terrain.HeightmapSize);
			Shader.SetGlobalInt("_HeightmapSizePadded", Terrain.HeightmapSizePadded);
			Shader.SetGlobalInt("_NormalmapSize", Terrain.NormalmapSize);
			Shader.SetGlobalInt("_NormalmapSizePadded", Terrain.NormalmapSizePadded);
			Shader.SetGlobalFloat("_HeightScale", Terrain.HeightScale);

			Shader.SetGlobalTexture("_TerrainAlbedo", albedoTexture2DArray);
			Shader.SetGlobalTexture("_TerrainNormal", normalTexture2DArray);
		}

		private void UpdateBuffers(List<QuadTreeNode> selection)
		{
			int instanceCount = Mathf.Min(selection.Count, Terrain.MaxSelectionCount);

			// Indirect args
			uint numIndices = (m_InstanceGridMesh != null) ? (uint)m_InstanceGridMesh.GetIndexCount(0) : 0;
			args[0] = numIndices;
			args[1] = (uint)instanceCount;
			m_ArgsBuffer.SetData(args);

			for (int i = 0; i < instanceCount; i++)
			{
				Bounds nodeBounds = selection[i].m_Bounds;
				positions[i] = new Vector4((float)nodeBounds.min.x, 0, (float)nodeBounds.min.z, nodeBounds.size.x);
				heightmapAtlasPos[i] = new Vector2(selection[i].HeightmapDescriptor.tl.x, selection[i].HeightmapDescriptor.tl.y);
				normalmapAtlasPos[i] = new Vector2(selection[i].NormalmapDescriptor.tl.x, selection[i].NormalmapDescriptor.tl.y);
			}

			m_PositionBuffer.SetData(positions, 0, 0, instanceCount);
			m_HeightmapAtlasPosBuffer.SetData(heightmapAtlasPos, 0, 0, instanceCount);
			m_NormalmapAtlasPosBuffer.SetData(normalmapAtlasPos, 0, 0, instanceCount);
		}


		private Mesh CreateGridMesh(int size)
		{
			Mesh grid = new Mesh();

			// Gen vertices
			Vector3[] vertices = new Vector3[(size + 1) * (size + 1)];
			Vector2[] uvs = new Vector2[vertices.Length];
			Vector4[] tangents = new Vector4[vertices.Length];
			Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);

			for (int i = 0, y = 0; y <= size; y++)
			{
				for (int x = 0; x <= size; x++, i++)
				{
					uvs[i] = new Vector2((float)x / (float)size, (float)y / (float)size);
					tangents[i] = tangent;
					vertices[i] = new Vector3((float)x / (float)size, 0.0f, (float)y / (float)size);
				}
			}

			int[] triangles = new int[size * size * 6];
			for (int ti = 0, vi = 0, y = 0; y < size; y++, vi++)
			{
				for (int x = 0; x < size; x++, ti += 6, vi++)
				{
					triangles[ti] = vi + size + 1;//vi;
					triangles[ti + 4] = triangles[ti + 1] = vi + size + 2;//vi + 1;
					triangles[ti + 3] = triangles[ti + 2] = vi;//vi + size + 1;
					triangles[ti + 5] = vi + 1;//vi + size + 2;
				}
			}

#if UNITY_2017_3_OR_NEWER
			if (size > 128)
				grid.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			else
				grid.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
#endif

			grid.vertices = vertices;
			grid.uv = uvs;
			grid.triangles = triangles;
			grid.tangents = tangents;
			grid.RecalculateNormals();
			grid.UploadMeshData(false);
			grid.RecalculateBounds();

			return grid;
		}


		void OnDisable()
		{
			if (m_PositionBuffer != null)
				m_PositionBuffer.Release();
			m_PositionBuffer = null;

			if (m_ArgsBuffer != null)
				m_ArgsBuffer.Release();
			m_ArgsBuffer = null;
		}

	}
}

