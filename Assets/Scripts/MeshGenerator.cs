using UnityEngine;
public abstract class MeshGenerator : MonoBehaviour
{
    abstract public void GenerateMesh(int[,] map, float
    squareSize);
}
