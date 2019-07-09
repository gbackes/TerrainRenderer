using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
	public class QuadTree
	{
		public Terrain Terrain { get; private set; }
		
		public int m_NodeCount = 0;
		public int MaxDepth { get; private set; } = 0;
		public int LevelCount { get; private set; } = 0;

		public QuadTreeNode m_Root = null;

		public double[] m_LevelMaxDiameter;
		#region Constructor

		public QuadTree(Terrain terrain)
		{
			Terrain = terrain;
			MaxDepth = (int)Mathf.Log(Terrain.TerrainSize, 2) - (int)Mathf.Log(Terrain.GridSize, 2);
			LevelCount = MaxDepth + 1;

			InitializeLevelMaxDiameterArray();

			Bounds terrainBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(Terrain.TerrainSize, 10, Terrain.TerrainSize));
			m_Root = QuadTreeNodePool.GetNode();
			m_Root.Initialize(this, null, terrainBounds);

		}

		#endregion

		void InitializeLevelMaxDiameterArray()
		{
			m_LevelMaxDiameter = new double[LevelCount];

			double terrainDiagonal = Math.Sqrt((double)Terrain.TerrainSize * (double)Terrain.TerrainSize * 2.0f);
			for (int i = MaxDepth; i >= 0; i--)
			{
				m_LevelMaxDiameter[i] = terrainDiagonal / Math.Pow(2, i);
			}
		}

		public void CollectOldNodes()
		{
			CollectOldNodes(m_Root);
		}

		private int CollectOldNodes(QuadTreeNode node)
		{
			if (node == null)
				return 0;

			//Não pode ser desalocado? Fazer algo

			if(node.IsLeaf() || !node.HasBeenRefined())
			{
				return node.m_SelectionTimestamp;
			}

			int mostRecentlySelected = 0;

			for (int i = 0; i < 4; i++)
			{
				mostRecentlySelected = Math.Max(mostRecentlySelected, CollectOldNodes(node.m_Children[i]));
			}

			if(Time.frameCount - mostRecentlySelected > Terrain.nodeCollectionThreshold)
			{
				node.ReleaseChildren();
				return Time.frameCount;
			}

			return mostRecentlySelected;
		}

		public void Reset()
		{
			MaxDepth = (int)Mathf.Log(Terrain.TerrainSize, 2) - (int)Mathf.Log(Terrain.GridSize, 2);
			LevelCount = MaxDepth + 1;

			InitializeLevelMaxDiameterArray();

			m_Root.Reset();
			Bounds terrainBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(Terrain.TerrainSize, 10, Terrain.TerrainSize));
			m_Root.Initialize(this, null, terrainBounds);
		}
	}
}
