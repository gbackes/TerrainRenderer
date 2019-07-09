using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Terrain
{
	public static class QuadtreeNodeGenerator {

		private static ComputeShader heightMapCompute = (ComputeShader)Resources.Load("HeightmapGenerator");
		private static ComputeShader normalMapCompute = (ComputeShader)Resources.Load("NormalmapGenerator");

		private static int heightmapKernel;
		private static int normalmapKernel;

		private static int[] buff = new int[2];

		public static void Initialize()
		{
			heightmapKernel = heightMapCompute.FindKernel("GenerateHeightmap");
			normalmapKernel = normalMapCompute.FindKernel("GenerateNormalmap");
		}
		public static void DispatchHeightmapKernel(QuadTreeNode node)
		{
			node.m_MinMaxBuffer.SetData(new int[] { int.MaxValue, int.MinValue });

			ComputeBuffer buffer = new ComputeBuffer(node.Terrain.noiseSettings.Length, 48);
			buffer.SetData(node.Terrain.noiseSettings);

			heightMapCompute.SetBuffer(heightmapKernel, "_NoiseSettings", buffer);
			heightMapCompute.SetInt("_NoiseCount", node.Terrain.noiseSettings.Length);

			heightMapCompute.SetFloat("_HeightScale", Terrain.HeightScale);

			heightMapCompute.SetInt("_HeightmapSize", Terrain.HeightmapSize);
			heightMapCompute.SetInt("_HeightmapSizePadded", Terrain.HeightmapSizePadded);

			heightMapCompute.SetVector("_HeightmapAtlasPos", new Vector2(node.HeightmapDescriptor.tl.x, node.HeightmapDescriptor.tl.y));
			heightMapCompute.SetVector("_NormalmapAtlasPos", new Vector2(node.NormalmapDescriptor.tl.x, node.NormalmapDescriptor.tl.y));

			heightMapCompute.SetTexture(heightmapKernel, "_HeightmapAtlas", node.HeightmapDescriptor.atlas.texture);
			heightMapCompute.SetTexture(normalmapKernel, "_NormalmapAtlas", node.NormalmapDescriptor.atlas.texture);

			heightMapCompute.SetVector("_NodePos", new Vector2(node.m_Bounds.min.x, node.m_Bounds.min.z));
			heightMapCompute.SetFloat("_NodeSize", node.m_Bounds.size.x);


			heightMapCompute.SetBuffer(heightmapKernel, "_MinMaxBuffer", node.m_MinMaxBuffer);
			
			int ngroups = Mathf.CeilToInt(node.HeightmapDescriptor.size / 8.0f);
			Stopwatch s = new Stopwatch();
			s.Start();
			heightMapCompute.Dispatch(heightmapKernel, ngroups, ngroups, 1);

			node.m_MinMaxBuffer.GetData(buff);
			s.Stop();
			node.UpdateMinMax(buff[0], buff[1]);
		}

		public static void DispatchNormalmapKernel(QuadTreeNode node)
		{

			normalMapCompute.SetVector("_HeightmapAtlasPos", new Vector2(node.HeightmapDescriptor.tl.x, node.HeightmapDescriptor.tl.y));
			normalMapCompute.SetVector("_NormalmapAtlasPos", new Vector2(node.NormalmapDescriptor.tl.x, node.NormalmapDescriptor.tl.y));
			
			normalMapCompute.SetInt("_NormalmapSizePadded", Terrain.NormalmapSizePadded); 

			normalMapCompute.SetFloat("_NodeSize", node.m_Bounds.size.x);

			normalMapCompute.SetTexture(normalmapKernel, "_HeightmapAtlas", node.HeightmapDescriptor.atlas.texture);
			normalMapCompute.SetTexture(normalmapKernel, "_NormalmapAtlas", node.NormalmapDescriptor.atlas.texture);

			normalMapCompute.SetBuffer(normalmapKernel, "_NormalmapReadyBuffer", node.m_NormalmapReadyBuffer);

			int ngroups = Mathf.CeilToInt(node.NormalmapDescriptor.size / 8.0f);
			normalMapCompute.Dispatch(normalmapKernel, ngroups, ngroups, 1);

			node.m_NormalmapReadyBuffer.GetData(buff);
		}
	}
}