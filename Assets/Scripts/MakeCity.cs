using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MakeCity : MonoBehaviour
{
    public GameObject[] buildings;
    void Start()
    {
        Perlin surface = new Perlin();
        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        float scalex = this.transform.localScale.x;
        float scalez = this.transform.localScale.z;
        for (int v = 0; v < vertices.Length; v++)
        {
            float perlinValue = surface.Noise(vertices[v].x * 2 + 0.1365143f,vertices[v].z * 2 + 1.21688f) * 10;
            perlinValue = Mathf.Round((Mathf.Clamp(perlinValue, 0, buildings.Length)));
            Instantiate(buildings[(int)perlinValue],
            new Vector3(vertices[v].x * scalex,
            vertices[v].y, vertices[v].z * scalez),
            buildings[(int)perlinValue].transform.rotation);
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        this.gameObject.AddComponent<MeshCollider>();
    }
}
