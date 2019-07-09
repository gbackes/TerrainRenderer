namespace Terrain
{
	using System.Linq;

	public static class QuadTreeNodePool
	{
		/// <summary>
		/// Maximum number of concurrent nodes in memory since startup.
		/// </summary>
		public static int maxCurrentNodeCount = 0;
		/// <summary>
		/// Number of nodes currently in memory.
		/// </summary>
		public static int currentNodeCount = 0;
		/// <summary>
		/// Total number of nodes built. Includes released nodes.
		/// </summary>
		public static int allTimeBuiltNodeCount = 0;
		/// <summary>
		/// Number of nodes available in the pool.
		/// </summary>
		public static int Count
		{
			get
			{
				return pos + 1;
			}
		}
		/*/// <summary>
		/// Use this to check if there are duplicated nodes in the pool.
		/// </summary>
		public static int UniqueCount
		{
			get
			{
				var groups = pool.GroupBy(v => v);
				int uniquecount = 0;
				foreach (var group in groups)
				{
					//Console.WriteLine("Value {0} has {1} items", group.Key, group.Count());
					if (group.Count() == 1)
						uniquecount++;
				}
				return uniquecount;
			}
		}*/

		public static int allTimeUsedNodes = 0;
		public static int nodesBuiltThisFrame = 0;

		private const int poolSize = 2048;
		private static QuadTreeNode[] pool = new QuadTreeNode[poolSize];
		private static int pos = -1;

		public static void Fill()
		{
			for (int i = 0; i < poolSize; i++)
			{
				pool[i] = new QuadTreeNode();
			}
			pos = poolSize - 1;
		}

		public static QuadTreeNode GetNode()
		{
			allTimeUsedNodes++;
			if (IsEmpty())
			{
				nodesBuiltThisFrame++;
				return new QuadTreeNode();
			}

			QuadTreeNode node = pool[pos];
			pool[pos] = null;
			pos--;
			return node;
		}

		public static void AddNode(QuadTreeNode node)
		{
			if (IsFull())
			{
				node.Release();
				return;
			}
		
			node.Reset();

			pos++;
			pool[pos] = node;
		}

		public static bool IsEmpty()
		{
			return pos == -1;
		}

		public static bool IsFull()
		{
			return pos == poolSize - 1;
		}

		public static void Clear()
		{
			for (int i = 0; i < pool.Length; i++)
			{
				if (pool[i] == null)
					continue;

				pool[i].Release();
				pool[i] = null;
			}
			pos = -1;
			maxCurrentNodeCount = 0;
			currentNodeCount = 0;
			allTimeBuiltNodeCount = 0;
		}

		public static void Update()
		{
			nodesBuiltThisFrame = 0;
		}
	}
}