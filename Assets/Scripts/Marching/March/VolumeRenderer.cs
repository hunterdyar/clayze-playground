using System;
using System.Collections.Generic;
using UnityEngine;

namespace Marching
{
	public class VolumeRenderer : MonoBehaviour
	{
		private Volume _volume;
		[SerializeField] private Transform _meshParent;
		[Min(1)][Tooltip("Chunks per axis.")]
		[SerializeField] private int _divisions;//not number of slices, but number of chunks per axis.

		[SerializeField] private Material _material;
		
		private int _chunkSize;//points per chunk in volume space.

		private readonly Dictionary<Vector3Int, GenerateMesh> _chunks = new Dictionary<Vector3Int, GenerateMesh>();
		[Header("Pass-Through Configuration")] public ComputeShader MarchingCompute;
		[Range(0, 1)] public float smoothness;
		public float SurfaceLevel = 0;

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
					c.UpdateMesh();
					c.DebugGizmoColor = Color.green;
				}
				else
				{
					c.DebugGizmoColor = Color.white;
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
						var gen = chunk.AddComponent<GenerateMesh>();
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