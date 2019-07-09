using System;
using UnityEngine;

namespace Terrain
{
	public class TerrainLODSettings
	{
		public Terrain Terrain { get; private set; }

		/// <summary>
		/// Stores the range of visibility of each lod level (quadtree level).
		/// The 'x' component stores the starting and 'y' the max view distance.
		/// </summary>
		public Vector2[] viewRanges;

		public float viewRangeMultiplier = 1.5f;

		public Plane[] frustumPlanes = new Plane[6];
		private Camera m_Cam;
		public Camera Camera
		{
			get
			{
				return m_Cam;
			}
			set
			{
				m_Cam = value;
				if (m_Cam != null)
					GeometryUtility.CalculateFrustumPlanes(m_Cam, frustumPlanes);
			}
		}

		public TerrainLODSettings(Terrain terrain)
		{
			Terrain = terrain;
			viewRanges = new Vector2[Terrain.m_QuadTree.LevelCount];
			GenerateViewRanges();
		}

		public void GenerateViewRanges()
		{
			for (int depth = Terrain.m_QuadTree.MaxDepth; depth >= 0; depth--)
			{
				if (depth == Terrain.m_QuadTree.MaxDepth)
				{
					viewRanges[depth].x = 0.0f;
					continue;
				}

				int prevDepth = depth + 1;
				viewRanges[depth].x = viewRanges[prevDepth].x * viewRangeMultiplier + (float)Terrain.m_QuadTree.m_LevelMaxDiameter[prevDepth];
				viewRanges[prevDepth].y = viewRanges[depth].x;

				if (depth == 0)
					viewRanges[depth].y = Single.MaxValue;
			}
		}
	}
}