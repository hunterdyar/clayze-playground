using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using Marching.Operations;

public class Volume : MonoBehaviour
{
    /// <summary>
    /// This is called whenever a command is executed, added, changed. It provides the min and max volume-space positions of the bounds of the operation.
    /// </summary>
    public Action<Vector3Int,Vector3Int> OnChange;

    //todo: Define in usable units: world size and resolution, then calculate size.
    public int TotalPointCoint => Size * Size * Size;
    
    public int Size => _size;
    [SerializeField][Min(2)] private int _size;
    public float pointsPerUnit = 1;
    public Operation[] operationObjects;

    private float _f;
    private float4[] data;

    public Texture3D testTexture;
    
    public void UpdateSize(int newSize)
    {
        if (_size == newSize )
        {
            return;
        }

        throw new NotImplementedException("don't change size during gameplay ... >:");
    }

    private void Start()
    {
        foreach (var op in operationObjects)
        {
            op.SetVolume(this);
        }
    }
    //normally we like a vector3int parameter for readability over garbage collection, but this get's called 'thousands of times a frame' not 'every frame' often.

    public float SamplePoint(int x, int y, int z)
    {
        //this needs to sample the point across all operations. 
        //foreach operation... point...
        _f = 0;
        foreach (var operation in operationObjects)
        {
            //todo: keep a list of active objects
            if (!operation.gameObject.activeInHierarchy)
            {
                continue;
            }
            _f = _f - operation.Sample(VolumeToWorld(x,y,z));
        }
        return _f;
    }

    public Vector3 VolumeToWorld(Vector3Int volPos)
    {
        return (Vector3)volPos/pointsPerUnit;
    }

    public Vector3 VolumeToWorld(int x, int y, int z)
    {
        return new Vector3(x / pointsPerUnit,y / pointsPerUnit,z / pointsPerUnit);
    }

    public Vector3Int WorldToLocal(Vector3 world)
    {
        world = transform.InverseTransformPoint(world)*pointsPerUnit;//local
        return new Vector3Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y), Mathf.FloorToInt(world.z));
    }

    public int IndexFromCoord(int x, int y, int z, int size)
    {
        return
            z * size * size +
            y * size +
            x;
    }

    public Vector3Int CoordFromIndex(int i)
    {
        int z = i / (Size * Size);
        int y = (i / Size) % Size;
        int x = i % Size;
        return new Vector3Int(x,y,z);
    }
    
    public void GenerateInBounds(ref ComputeBuffer pointsBuffer, Vector3Int min, Vector3Int max)
    {
        //can we cache the chunkSize? or just pass that in? Then we could make data once, and not collect garbage
        int size = max.x - min.x;//assume square.
        data = new float4[size*size*size]; //if PointCount changes, this has to be updated.
        //todo: bounds
        if (!pointsBuffer.IsValid())
        {
            Debug.LogError("Invalid Points Buffer? Why");
            return;
        }
        pointsBuffer.SetCounterValue(0);
        
        int index = 0;
        //todo: hey, these are always square! Origin+Size, not min/max.
        for (int dx = 0; dx < size; dx++)
        {
            for (int dy = 0; dy < size; dy++)
            {
                for (int dz = 0; dz < size; dz++)
                {
                    // I think if we change this to a ushort,ushort,ushort,float; anything to reduce stride count, will help.
                    //to do that, we have to make a struct and a matching one on the GPU - see Triangle.
                    var p = VolumeToWorld(dx+min.x,dy+min.y,dz+min.z);
                    //hypothetically we are looping in the appropriate order such that we don't need to do IndexFromCoord.
                    //THis will stop being true later, so I'm leaving it 
                    data[IndexFromCoord(dx, dy, dz,size)] =
                        new float4((float)p.x, (float)p.y, (float)p.z, SamplePoint(dx+min.x,dy+min.y,dz+min.z));
                    index++;
                }
            }
        }
        //stride is 16. float is 4 bytes, float4 = 16.
        pointsBuffer.SetData(data,0,0,data.Length);
    }

    [ContextMenu("Save Test Texture")]
    private void SaveToTexture3D()
    {
        var size = Size;
        var min = Vector3Int.zero;
        var max = new Vector3Int(size, size, size);
        
        for (int dx = 0; dx < size; dx++)
        {
            for (int dy = 0; dy < size; dy++)
            {
                for (int dz = 0; dz < size; dz++)
                {
                    // I think if we change this to a ushort,ushort,ushort,float; anything to reduce stride count, will help.
                    //to do that, we have to make a struct and a matching one on the GPU - see Triangle.
                    var p = VolumeToWorld(dx + min.x, dy + min.y, dz + min.z);
                    //hypothetically we are looping in the appropriate order such that we don't need to do IndexFromCoord.
                    //THis will stop being true later, so I'm leaving it 
                    testTexture.SetPixel(dx,dy,dz,Color.Lerp(Color.black,Color.white,
                        SamplePoint(dx + min.x, dy + min.y, dz + min.z)));
                }
            }
        }
    }
    public void OperationChanged((Vector3, Vector3) worldBounds)
    {
        OnChange?.Invoke(WorldToLocal(worldBounds.Item1),WorldToLocal(worldBounds.Item2));
    }
}