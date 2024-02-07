using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Object = System.Object;

namespace Marching
{
	public class GenerateMesh : MonoBehaviour
	{
		//setup
		public bool ConstantRefresh;
		private Volume _volume;
		private VolumeRenderer _volumeRenderer;

		private ComputeShader MarchingCubeCompute => _volumeRenderer.MarchingCompute;
		private float SurfaceLevel => _volumeRenderer.SurfaceLevel;
		private float Smoothness => _volumeRenderer.smoothness;
		
		public Vector3Int PointsMin { get; private set; }
		public Vector3Int PointsMax { get; private set; }
		private int Size;
		
		public Vector3Int Coord { get; set; }

		[HideInInspector]
		public Color DebugGizmoColor = Color.white;

		private MeshRenderer _meshRenderer;
		private MeshFilter _meshFilter;
		
		//settings
		private int _threadsPerAxis;
		private bool updatedThisFrame;

		public Vector3 WorldCenter => _worldCenter;
		private Vector3 _worldCenter;
		
		//Properties
		ComputeBuffer _pointsBuffer;
		GraphicsBuffer _triangleBuffer;
		GraphicsBuffer _triangleCountBuffer;
		private CommandBuffer _computeCommandBuffer;
		
		private Mesh _mesh;
		
		//shader prop cache
		private static readonly int PointsPropName = Shader.PropertyToID("points");
		private static readonly int TrianglesPropName = Shader.PropertyToID("triangles");
		private static readonly int PointsPerAxisPropName = Shader.PropertyToID("pointsPerAxis");
		private static readonly int LevelPropName = Shader.PropertyToID("surfaceLevel");
		private static readonly int SmoothnessPropName = Shader.PropertyToID("smoothness");
		
		private void Awake()
		{
			_meshFilter = GetComponent<MeshFilter>();
			_meshRenderer = GetComponent<MeshRenderer>();
			
			//create a new mesh to do out thing
			_mesh = new Mesh
			{
				indexFormat = IndexFormat.UInt32
			};
			_meshFilter.sharedMesh = _mesh;
		}

		public void Initialize(VolumeRenderer volumeRenderer, Volume volume, Vector3Int min, Vector3Int max)
		{
			_volumeRenderer = volumeRenderer;
			_volume = volume;
			PointsMin = min;
			PointsMax = max;
			Size = max.x-min.x;
			
			CreateBuffers();
			ConfigureThreadsPerAxis();
			_worldCenter = _volume.transform.TransformPoint((_volume.VolumeToWorld(PointsMax - Vector3Int.one) +
			                                                 _volume.VolumeToWorld(PointsMin)) / 2);
			UpdateMesh();

		}

		private void Start()
		{
		}

		private void Update()
		{
			if (ConstantRefresh)
			{
				UpdateMesh();
			}
		}

		void CreateBuffers()
		{
			int numPoints = _volume.TotalPointCoint;
			int voxelsPerAxis = _volume.Size - 1;//points are volume determined
			int numVoxels = voxelsPerAxis * voxelsPerAxis * voxelsPerAxis;
			int maxTriangleCount = numVoxels * 5;//5 is max triangles per cube march shape... I believe.
			
			if (_pointsBuffer == null || numPoints != _pointsBuffer.count)
			{
				ReleaseBuffers();
				_triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append,maxTriangleCount, sizeof(float) * 3 * 3);
				_pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4,ComputeBufferType.Append);
				_triangleCountBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw,1, sizeof(int));
				_computeCommandBuffer = new CommandBuffer();
			}
		}

		public void ConfigureThreadsPerAxis()
		{
			int voxelsPerAxis = Size - 1; //hello, fence post
			int threadGroupSize = 1;
			_threadsPerAxis = Mathf.CeilToInt(voxelsPerAxis / (float)threadGroupSize);
			//it breaks when numThreadsPerAxis is 1,1,1. So we're just gonna set the min to 2.
			_threadsPerAxis = _threadsPerAxis == 1 ? 2 : _threadsPerAxis;
			//float surfaceLevel = SurfaceLevel;
		}
		
		[ContextMenu("Update Mesh")]
		public void UpdateMesh(bool async = false)
		{
            //Update the points buffer from the volume. Processes and copies data from CPU to GPU.
            _volume.GenerateInBounds(ref _pointsBuffer,PointsMin,PointsMax);
            
            _triangleBuffer.SetCounterValue(0);//computer buffer uses appends
            MarchingCubeCompute.SetBuffer(0, PointsPropName, _pointsBuffer);
            MarchingCubeCompute.SetBuffer(0, TrianglesPropName, _triangleBuffer);
            MarchingCubeCompute.SetInt(PointsPerAxisPropName, Size);//volume.size
            MarchingCubeCompute.SetFloat(LevelPropName, SurfaceLevel);
            MarchingCubeCompute.SetFloat(SmoothnessPropName,Smoothness);
            //do the thing.
            if (!async || !SystemInfo.supportsAsyncCompute)
            {
	            MarchingCubeCompute.Dispatch(0, _threadsPerAxis, _threadsPerAxis, _threadsPerAxis);
	            StartCoroutine(CreateMesh());
            }
            else
            {
	            _computeCommandBuffer.DispatchCompute(MarchingCubeCompute,0, _threadsPerAxis, _threadsPerAxis,
		            _threadsPerAxis);
	            Graphics.ExecuteCommandBufferAsync(_computeCommandBuffer,ComputeQueueType.Default);
	            //AsyncGPUReadback.Request(MarchingCubeCompute, CreateMesh);
	            //when that's done...
	            StartCoroutine(CreateMesh());
            }
		}

		public IEnumerator CreateMesh()
		{
			//todo: use a graphics fence to stop the gpu to wait for the cpu?
			
			// Get number of triangles in the triangle buffer
			GraphicsBuffer.CopyCount(_triangleBuffer, _triangleCountBuffer, 0);
			int[] triCountArray = { 0 }; //the buffer is just an array of a thing
			_triangleCountBuffer.GetData(triCountArray);
			int numTris = triCountArray[0];

			// Get triangle data from shader
			Triangle[] tris = new Triangle[numTris];
			_triangleBuffer.GetData(tris, 0, 0, numTris);
			_mesh.Clear();

			var vertices = new Vector3[numTris * 3];
			var meshTriangles = new int[numTris * 3];

			for (int i = 0; i < numTris; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					meshTriangles[i * 3 + j] = i * 3 + j;
					vertices[i * 3 + j] = tris[i][j]; //times 2?
				}
			}

			_mesh.vertices = vertices;
			_mesh.triangles = meshTriangles;

			_mesh.RecalculateNormals();
			// _mesh.RecalculateBounds();
			yield break;
		}

		void ReleaseBuffers()
		{
			if (_triangleBuffer != null)
			{
				_triangleBuffer.Release();
			}

			if (_pointsBuffer != null)
			{
				_pointsBuffer.Release();
			}

			if (_triangleCountBuffer != null)
			{
				_triangleCountBuffer.Release();
			}

			if (_computeCommandBuffer != null)
			{
				_computeCommandBuffer.Release();
			}
		}

		private void OnDestroy()
		{
			ReleaseBuffers();
		}

		private void OnDrawGizmos()
		{
			//We secretly draw them too small, because they share boundries with their neighbors, the overlapping is a visual mess in the inspector to look at.
			//for debugging, the color change is accurate, that's determined elsewhere.
			
			//offset interior meshes...
			float size = (Size-1) / _volume.pointsPerUnit;
			Gizmos.color = new Color(DebugGizmoColor.r, DebugGizmoColor.g, DebugGizmoColor.b, 0.2f);
			Gizmos.DrawWireCube(WorldCenter, new Vector3(size, size, size));
		}
	}
}