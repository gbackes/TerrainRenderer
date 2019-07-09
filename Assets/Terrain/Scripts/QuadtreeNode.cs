using UnityEngine;


namespace Terrain
{
	public class QuadTreeNode
	{
		private QuadTree m_QuadTree;
		private QuadTreeNode m_Parent = null;
		public QuadTreeNode[] m_Children = new QuadTreeNode[4];

		public int m_Level;
		public int m_SelectionTimestamp;

		public Bounds m_Bounds;
		public const float DefaultMinHeight = -8000f;
		public const float DefaultMaxHeight = 8000f;
		public float m_MinHeight = DefaultMinHeight;
		public float m_MaxHeight = DefaultMaxHeight;

		public Terrain Terrain
		{
			get
			{
				return m_QuadTree.Terrain;
			}
		}

		private Atlas.AtlasPageDescriptor m_HeightmapDescriptor;
		public Atlas.AtlasPageDescriptor HeightmapDescriptor
		{
			get
			{
				if (m_HeightmapDescriptor == null)
					m_HeightmapDescriptor = m_QuadTree.Terrain.m_HeightmapAtlas.GetPage();

				return m_HeightmapDescriptor;
			}
		}

		private Atlas.AtlasPageDescriptor m_NormalmapDescriptor;
		public Atlas.AtlasPageDescriptor NormalmapDescriptor
		{
			get
			{
				if (m_NormalmapDescriptor == null)
					m_NormalmapDescriptor = m_QuadTree.Terrain.m_NormalmapAtlas.GetPage();

				return m_NormalmapDescriptor;
			}
		}


		public ComputeBuffer m_NormalmapReadyBuffer = new ComputeBuffer(2, sizeof(int));
		public ComputeBuffer m_MinMaxBuffer = new ComputeBuffer(2, sizeof(int));

		public QuadTreeNode()
		{
			QuadTreeNodePool.allTimeBuiltNodeCount++;
			QuadTreeNodePool.currentNodeCount++;
		//	QuadTreeNodePool.maxCurrentNodeCount = QuadTreeNodePool.currentNodeCount > QuadTreeNodePool.maxCurrentNodeCount ? QuadTreeNodePool.currentNodeCount : QuadTreeNodePool.maxCurrentNodeCount;
		}

		public void Initialize(QuadTree quadtree, QuadTreeNode parent, Bounds bounds)
		{
			m_QuadTree = quadtree;
			m_QuadTree.m_NodeCount++;

			m_Bounds = bounds;
			m_Parent = parent;

			m_Level = parent != null ? parent.m_Level + 1 : 0;

			m_HeightmapDescriptor = Terrain.m_HeightmapAtlas.GetPage();
			m_NormalmapDescriptor = Terrain.m_NormalmapAtlas.GetPage();

			QuadtreeNodeGenerator.DispatchHeightmapKernel(this);
			QuadtreeNodeGenerator.DispatchNormalmapKernel(this);

		}

		public bool IsLeaf()
		{
			return m_Level == m_QuadTree.MaxDepth;
		}

		public bool HasBeenRefined()
		{
			return (m_Children[0] != null && m_Children[1] != null && m_Children[2] != null && m_Children[3] != null);
		}

		public void UpdateMinMax(float minHeight, float maxHeight)
		{
			m_MinHeight = m_MinHeight == DefaultMinHeight ? minHeight : Mathf.Min(minHeight, m_MinHeight);
			m_MaxHeight = m_MaxHeight == DefaultMaxHeight ? maxHeight : Mathf.Max(maxHeight, m_MaxHeight);

			float height = (m_MaxHeight - m_MinHeight);
			float centerHeight = m_MinHeight + height / 2;

			Vector3 boundsCenter = new Vector3(m_Bounds.center.x, centerHeight, m_Bounds.center.z);
			Vector3 boundsSize = new Vector3(m_Bounds.size.x, height, m_Bounds.size.z);

			m_Bounds = new Bounds(boundsCenter, boundsSize);

			Terrain.MaxHeight = Mathf.Max(Terrain.MaxHeight, m_MaxHeight);
			Terrain.MinHeight = Mathf.Min(Terrain.MinHeight, m_MinHeight);
			if (m_Parent != null) 
				m_Parent.UpdateMinMax(m_MinHeight, m_MaxHeight);
		}

		public void Release()
		{
			Reset();

			if (m_MinMaxBuffer != null)
				m_MinMaxBuffer.Release();
			m_MinMaxBuffer = null;
			if (m_NormalmapReadyBuffer != null)
				m_NormalmapReadyBuffer.Release();
			m_NormalmapReadyBuffer = null;

			m_Children = null;
		}

		public void Reset()
		{
			ReleaseChildren();

			if (m_Parent != null)
			{
				for (int i = 0; i < 4; i++)
				{
					if (m_Parent.m_Children[i] == this)
						m_Parent.m_Children[i] = null;
				}
			}
			
			ReleaseHeightmap();
			ReleaseNormalmap();

			m_Parent = null;

			if (m_QuadTree != null)
				m_QuadTree.m_NodeCount--;
			m_QuadTree = null;

			m_MinHeight = DefaultMinHeight;
			m_MaxHeight = DefaultMaxHeight;
			//Why?
			m_SelectionTimestamp = Time.frameCount;
		}

		public void ReleaseChildren()
		{
			if (HasBeenRefined())
			{
				for (int i = 0; i < 4; i++)
				{
					QuadTreeNodePool.AddNode(m_Children[i]);
					m_Children[i] = null;
				}
			}
		}

		public void Refine()
		{
			for (int i = 0; i < 4; i++)
			{
				if (m_Children[i] == null)
					m_Children[i] = QuadTreeNodePool.GetNode();

				Bounds childBounds;
				GetChildBounds(i, out childBounds);
				m_Children[i].Initialize(m_QuadTree, this, childBounds);
			}
		}

		private void GetChildBounds(int childIndex, out Bounds childBounds)
		{
			Vector3 extends = m_Bounds.extents;
			Vector3 childrenSize = new Vector3(extends.x, 10, extends.z);
			Vector3 center = new Vector3(m_Bounds.center.x, 0, m_Bounds.center.z);
			Vector3 position;
			switch (childIndex)
			{
				case 0:
					position = new Vector3(-extends.x, 0, -extends.z) / 2;
					break;
				case 1:
					position = new Vector3(-extends.x, 0, extends.z) / 2;
					break;
				case 2:
					position = new Vector3(extends.x, 0, extends.z) / 2;
					break;
				case 3:
					position = new Vector3(extends.x, 0, -extends.z) / 2;
					break;
				default:
					position = Vector3.zero;
					break;
			}
			childBounds = new Bounds(center + position, childrenSize);
		}


		public void ReleaseHeightmap()
		{
			if (m_HeightmapDescriptor != null)
				m_HeightmapDescriptor.Release();
			m_HeightmapDescriptor = null;
		}
		public void ReleaseNormalmap()
		{
			if (m_NormalmapDescriptor != null)
				m_NormalmapDescriptor.Release();
			m_NormalmapDescriptor = null;
		}





	}
}