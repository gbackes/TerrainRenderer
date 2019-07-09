using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Terrain
{
	[Serializable]
	public struct NoiseStruct
	{
		public float Seed;
		public int Octaves;
		public float Frequency;
		public float Lacunarity;
		public float Amplitude;
		public float Gain;
		public float PerturbFeatures;
		public float Sharpness;
		public float AltitudeErosion;
		public float RidgeErosion;
		public float SlopeErosion;
		public float ConcavityErosion;
	}

	public class Terrain : MonoBehaviour
	{
		public Texture2D[] albedoTextures;
		public Texture2D[] normalTextures;
		public const int TextureArrayRes = 1024;
		public bool freezeSelection = false;
		public bool drawBounds = false;
		public bool debugMode = false;

		public List<QuadTreeNode> m_LODSelection;
		public QuadTree m_QuadTree;
		public TerrainLODSettings m_TerrainLODSettings;
		public TerrainRenderer m_TerrainRenderer;

		public const int MaxSelectionCount = 1024;

		public static int TerrainSize = 32768;
		public static int GridSize = 128;
		public static int HeightmapSize = GridSize + 1;
		public static int NormalmapSize = GridSize + 1;
		public static int HeightmapSizePadded = HeightmapSize + 2;
		public static int NormalmapSizePadded = NormalmapSize + 2;
		public static float HeightScale = 1024;

		public float MinHeight = 8000;
		public float MaxHeight = -8000;

		public Material instanceMaterial;

		public Atlas m_HeightmapAtlas;
		public Atlas m_NormalmapAtlas;

		public int nodeCollectionThreshold = 180;
		public int nodeCollectionInterval = 120;
		private float lastMemCollectionTime = 0f;

		[Header("Noise Settings")]
		public NoiseStruct[] noiseSettings;


		private void Start()
		{
			m_HeightmapAtlas = new Atlas(RenderTextureFormat.RFloat, FilterMode.Point, 8192, HeightmapSizePadded, true);
			m_NormalmapAtlas = new Atlas(RenderTextureFormat.ARGBHalf, FilterMode.Point, 8192, NormalmapSizePadded, true);

			QuadTreeNodePool.Fill();
			QuadtreeNodeGenerator.Initialize();

			m_QuadTree = new QuadTree(this);
			m_TerrainLODSettings = new TerrainLODSettings(this);
			m_TerrainRenderer = new TerrainRenderer(this);

			m_LODSelection = new List<QuadTreeNode>(MaxSelectionCount);

			SetDebugMode();

		}

		private void Update()
		{

			if (Input.GetKeyUp(KeyCode.N))
			{
				freezeSelection = !freezeSelection;
			}
			if (Input.GetKeyUp(KeyCode.B))
			{
				drawBounds = !drawBounds;
			}
			if (Input.GetKeyUp(KeyCode.Space))
			{
				m_QuadTree.Reset();
				m_TerrainLODSettings.GenerateViewRanges();
				MinHeight = 8000;
				MaxHeight = -8000;

			}
			if (Input.GetKeyUp(KeyCode.M))
			{
				debugMode = !debugMode;
				SetDebugMode();
			}


			QuadTreeNodePool.Update();

		}

		private void FixedUpdate()
		{
			if (freezeSelection) return;
			if (Time.frameCount - lastMemCollectionTime >= nodeCollectionInterval)
			{
				m_QuadTree.CollectOldNodes();
				lastMemCollectionTime = Time.frameCount;
			}
			
		}
		

		private void SetDebugMode()
		{
			if (debugMode)
			{
				Shader.EnableKeyword("_DEBUG_MODE");
			}
			else
			{
				Shader.DisableKeyword("_DEBUG_MODE");
			}
		}

		private void OnEnable()
		{
			Camera.onPreCull += MyPreCull;
		}

		private void OnDisable()
		{
			Camera.onPreCull -= MyPreCull;
		}

		private void MyPreCull(Camera cam)
		{
			// We don't want to select and render if the camera is set to cull the current layer of this object.
			if ((cam.cullingMask & (1 << gameObject.layer)) == 0)
				return;

			//if (cam.cameraType == CameraType.SceneView)
			//	DoSomething();

			if (!freezeSelection)
			{
				m_TerrainLODSettings.Camera = cam;
				QuadTreeTraversal.LODSelect(m_LODSelection, m_TerrainLODSettings);
			}

			m_TerrainRenderer.Render(cam, m_LODSelection);
		}

		private void OnDrawGizmos()
		{
			if (m_LODSelection == null || !drawBounds) return;
			foreach (QuadTreeNode node in m_LODSelection)
			{
				Gizmos.color = Color.Lerp(Color.red, Color.green, (float)node.m_Level / (float)m_QuadTree.LevelCount) * 2;
				Gizmos.DrawWireCube(node.m_Bounds.center, node.m_Bounds.size);
			}
		}

		private void OnValidate()
		{
			if (m_QuadTree == null) return;

			m_QuadTree.Reset();
			m_TerrainLODSettings.GenerateViewRanges();
			MinHeight = 8000;
			MaxHeight = -8000;
		}

	}
}
