using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Terrain
{
	public static class QuadTreeTraversal
	{
		public static void LODSelect(List<QuadTreeNode> selection, TerrainLODSettings settings)
		{
			if (selection == null || settings == null)
				return;

			selection.Clear();

			LODSelect(settings.Terrain.m_QuadTree.m_Root, selection, settings);
		}


		private static void LODSelect(QuadTreeNode node, List<QuadTreeNode> selection, TerrainLODSettings settings)
		{
			if (node == null || selection.Count >= selection.Capacity)
				return;
			
			node.m_SelectionTimestamp = Time.frameCount;

			// This lod level starting view distance
			float startingViewDist = settings.viewRanges[node.m_Level].x;

			Bounds nodebounds = node.m_Bounds;

			// Frustum culling
			if (!GeometryUtility.TestPlanesAABB(settings.frustumPlanes, nodebounds))
				return;

			// Only subdivide if this is not a leaf and it is out its range
			if (!node.IsLeaf() && Mathf.Sqrt(nodebounds.SqrDistance(settings.Camera.transform.position)) < startingViewDist)
			{
				if (!node.HasBeenRefined())
				{
					node.Refine();
					selection.Add(node);
				}
				else
				{
					for (int i = 0; i < 4; i++)
					{
						LODSelect(node.m_Children[i], selection, settings);
					}
				}
			}
			else
			{
				selection.Add(node);
			}
		}
	}
}
