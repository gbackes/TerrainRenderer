using System.Collections.Generic;
using System;
using UnityEngine;
using GlmSharp;

[Serializable]
public class Atlas
{
	public class AtlasPageDescriptor
	{
		public Atlas atlas;
		public ivec2 tl;
		public int size;

		public AtlasPageDescriptor(Atlas atlas, ivec2 tl, int size)
		{
			this.atlas = atlas;
			this.tl = tl;
			this.size = size;
		}

		public bool IsValid()
		{
			return atlas != null;
		}

		public void Release()
		{
			atlas.ReleasePage(this);
			atlas = null;
		}
	}

	public RenderTexture texture;
	//private RenderTexture clearTex;

	private Stack<ivec2> freePages;
	public int FreePageCount
	{
		get
		{
			return freePages.Count;
		}
	}

	private int m_PageSize;
	public int PageSize
	{
		get
		{
			return m_PageSize;
		}
	}

	private int m_PageCount;
	public int PageCount
	{
		get
		{
			return m_PageCount;
		}
	}

	private int m_PageCountDim;

	private bool m_Linear;

	public Atlas(RenderTextureFormat format, FilterMode filterMode, int res, int pageSize, bool linear)
	{
		if (res < pageSize)
			throw new InvalidOperationException("Atlas size must fit at least one page.");

		Create(format, filterMode, res, pageSize, linear);	
	}

	private void Create(RenderTextureFormat format, FilterMode filterMode, int res, int pageSize, bool linear)
	{
		texture = new RenderTexture(res, res, 0, format, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
		texture.enableRandomWrite = true;
		texture.autoGenerateMips = false;
		texture.filterMode = filterMode;
		texture.wrapMode = TextureWrapMode.Clamp;

		texture.Create();

		m_Linear = linear;
		m_PageSize = pageSize;
		m_PageCountDim = Mathf.FloorToInt(res / (float)m_PageSize);
		m_PageCount = m_PageCountDim * m_PageCountDim;

		freePages = new Stack<ivec2>(m_PageCount);
		for (int i = m_PageCountDim - 1; i >= 0; i--)
			for (int j = m_PageCountDim - 1; j >= 0; j--)
				freePages.Push(new ivec2(i, j));

		//clearTex = new RenderTexture(pageSize, pageSize, 0, format, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
		//clearTex.enableRandomWrite = true;
		//clearTex.autoGenerateMips = false;
		//clearTex.filterMode = FilterMode.Point;
		//clearTex.wrapMode = TextureWrapMode.Clamp;
	}

	public AtlasPageDescriptor GetPage()
	{
		if (freePages.Count == 0)
			return null;

		ivec2 page = freePages.Pop();
		page *= m_PageSize;

		//if (page.x != 0)
		//	page.x = page.x + 1;
		//if (page.y != 0)
		//	page.y = page.y + 1;

		return new AtlasPageDescriptor(this, page, m_PageSize);
	}

	public void ReleasePage(AtlasPageDescriptor page)
	{
		if (page.atlas != this)
			throw new InvalidOperationException("Wrong atlas.");

		//Graphics.CopyTexture(clearTex, 0, 0, 0, 0, m_PageSize, m_PageSize, texture, 0, 0, page.tl.x, page.tl.y);

		freePages.Push(page.tl / m_PageSize);
	}

	public bool IsFull()
	{
		return freePages.Count == 0;
	}

	public void Reset()
	{
		RenderTextureFormat format = texture.format;
		FilterMode filterMode = texture.filterMode;
		int res = texture.width;
		Release();
		Create(format, filterMode, res, m_PageSize, m_Linear);
	}

	public void Release()
	{
		texture.Release();
		texture = null;
		freePages.Clear();
	}
}


//public static class AtlasManager
//{
//	private struct AtlasDesc
//	{
//		public RenderTextureFormat format;
//		public int pageSize;
//		public bool linear;

//		public AtlasDesc(RenderTextureFormat format, int pageSize, bool linear)
//		{
//			this.format = format;
//			this.pageSize = pageSize;
//			this.linear = linear;
//		}
//	}

//	private static Dictionary<AtlasDesc, Atlas> atlases = new Dictionary<AtlasDesc, Atlas>();

//	public const int DefaultRes = 4096;

//	public static Atlas GetAtlas(RenderTextureFormat format, int pageSize, bool linear)
//	{
//		AtlasDesc desc = new AtlasDesc(format, pageSize, linear);

//		if (atlases.ContainsKey(desc))
//			return atlases[desc];

//		Atlas atlas = new Atlas(format, DefaultRes, pageSize, linear);
//		atlases[desc] = atlas;
//		return atlas;
//	}
//}
