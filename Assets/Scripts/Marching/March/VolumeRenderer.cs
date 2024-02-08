using System;
using System.Collections.Generic;
using UnityEngine;

namespace Marching
{
	/// <summary>
	/// A Volume Renderer Instantiates a number of VolumeChunks, according to Division. These chunks get the mesh renderer, and update themselves with their own pointcache's and compute shader dispatches.
	/// The Renderer listens for volume updates, and tells the appropriate chunks to update using unrotated bounding boxes.
	/// The number of chunk upates per frame will affect performance. Chunks are sorted so that the ones closest to the camera update first.
	/// Future optimizations....
	///   - having chunks know if they are full or empty. If so, they can skip processing. If they are full, we can start occlusion culling or de-prioritizing updates.
	///   - Frustum culling for chunk updates. Or even a naieve 'is behind the camera' plane culling: closest to camera sort does not know direction.
	///   - 
	/// </summary>
	[RequireComponent(typeof(Volume))]
	public class VolumeRenderer : MonoBehaviour
	{
		private Volume _volume;
		[SerializeField] private Transform _meshParent;
		[Min(1)][Tooltip("Chunks per axis.")]
		[SerializeField] private int _divisions;//not number of slices, but number of chunks per axis.
		[SerializeField] private Material _material;
		[Min(1)]
		[SerializeField] private int chunkUpdatesPerFrame;
		
		private int _chunkSize;//points per chunk in volume space.

		private readonly Dictionary<Vector3Int, VolumeChunk> _chunks = new Dictionary<Vector3Int, VolumeChunk>();
		[Header("Pass-Through Configuration")] public ComputeShader MarchingCompute;
		[Range(0, 1)] public float smoothness;
		public float SurfaceLevel = 0;

		private readonly List<VolumeChunk> _chunkNeedingUpdate = new List<VolumeChunk>();
		//Debugging
		private Vector3Int _lastEditMin;
		private Vector3Int _lastEditMax;
		private void Awake()
		{
			_volume = GetComponent<Volume>();
			GenerateChunks();
		}

		private void OnEnable()
		{
			_volume.OnChange += VolumeChange;
		}
		
		private void OnDisable()
		{
			_volume.OnChange -= VolumeChange;
		}

		private void VolumeChange(Vector3Int boundsMin, Vector3Int boundsMax)
		{
			//debugging, used to draw gizmos.
			_lastEditMin = boundsMin;
			_lastEditMax = boundsMax;
			
			
			//for every chunk overlapping the bounds of what changed, update it.
			foreach (var c in _chunks.Values)
			{
				if (GeometryUtility.CubesIntersect(c.PointsMin, c.PointsMax, boundsMin, boundsMax))
				{
					if (!_chunkNeedingUpdate.Contains(c))
					{
						_chunkNeedingUpdate.Add(c);
					}
					c.DebugGizmoColor = Color.green;
				}
				else
				{
					c.DebugGizmoColor = Color.white;
				}
			}
		}

		private void Update()
		{
			//todo: we would like to keep the chunks in a sorted list, and only update the ones closest to camera.
			//todo: we would like to isolated-update the chunks that are out of the camera frustum, on the interior of meshes, empty, or otherwise irrelevant. we can't do this chunk-wise (chunks might be empty). 
			
			int count = _chunkNeedingUpdate.Count;
			if (count > 0)
			{
				for (int i = Mathf.Min(count, chunkUpdatesPerFrame)-1; i >= 0; i--)
				{
					_chunkNeedingUpdate[i].UpdateMesh(true);
					_chunkNeedingUpdate.RemoveAt(i);//we loop through the list in reverse in order to modify it as we go.
					//this has the unintended consequence of a FILO setup. Will that be a problem? maybe!
				}
			}
			
			
		}

		public void GenerateChunks()
		{
			//todo: runtime reset, destroy children.
			
			_chunks.Clear();
			if (_volume.Size % _divisions != 0)
			{
				Debug.LogWarning("Bad Chunk Size. Must be even division of size.");
			}
			// ReSharper disable once PossibleLossOfFraction
			_chunkSize = Mathf.CeilToInt(_volume.Size / _divisions);//we should just divide and check if it's a perfect division or not.

			for (int i = 0; i < _divisions; i++)
			{
				for (int j = 0; j < _divisions; j++)
				{
					for (int k = 0; k < _divisions; k++)
					{
						Vector3Int chunkPos = new Vector3Int(i, j, k);

						GameObject chunk = new GameObject();
						chunk.transform.SetParent(_meshParent);
						chunk.name = $"Chunk - {i},{j},{k}";
						chunk.AddComponent<MeshFilter>();
						var mr = chunk.AddComponent<MeshRenderer>();
						mr.material = _material;
						var gen = chunk.AddComponent<VolumeChunk>();
						gen.SetVolumeRenderer(this,_volume);

						//Set appropriate points bounds.
						var min = new Vector3Int(i * _chunkSize, j * _chunkSize, k * _chunkSize);
						gen.PointsMin = min;
						gen.PointsMax = new Vector3Int(min.x + _chunkSize, min.y + _chunkSize, min.z + _chunkSize)+Vector3Int.one;
						_chunks.Add(chunkPos,gen);
					}
				}
			}
		}

		private void OnDrawGizmos()
		{
			if (_volume == null)
			{
				_volume = GetComponent<Volume>();
			}
			Gizmos.color = Color.red;
			Vector3 a = _volume.VolumeToWorld(_lastEditMin);
			Vector3 b = _volume.VolumeToWorld(_lastEditMax);
			
			Gizmos.DrawWireCube((a + b) / 2, b - a);
		}
	}
}