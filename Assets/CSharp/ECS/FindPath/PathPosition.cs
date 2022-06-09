using Unity.Entities;
using Unity.Mathematics;


[InternalBufferCapacity(40)]
public struct PathPosition : IBufferElementData
{
    public int2 position;
}
