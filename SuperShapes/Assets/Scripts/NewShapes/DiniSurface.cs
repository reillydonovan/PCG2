﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiniSurface : MonoBehaviour
{
    public int resolution = 50;

    // number of verts along the 'longitude'
    public int phiDivs = 10;
    // number of verts along the 'latitude'
    public int thetaDivs = 10;

    public bool bottleShape = false;
    public bool grayBottle = false;
    float umin = -1 * Mathf.PI;
    float umax = Mathf.PI;
    float vmin = -1 * Mathf.PI;
    float vmax = Mathf.PI;

    public float a = 0.0f;
    public float b = 0.0f;
    public float u = 2.0f;
    public float v = 0.0f;
    public float r = 50.0f;
    public float m2 = 0.0f;
    public float n = 0.0f;

    public float x = 0.0f;
    public float y = 0.0f;
    public float z = 0.0f;

    // of the sphere
    public int xMod1Period = 4; //how many ups and downs there are
    public float xMod1PhaseOffset = 1.0f; //how far the wave is shifted
    public float xMod1Scale = 2.0f; //how big the wave is
    public float xMod1YOffset = 1.1f; //how big the base of the wave is
    public float xMod1TimeResponse = 1.0f; //the amount the wave moves with time

    //set of vars exposed to create waves parallel to the 'longitude'
    // of the sphere
    public int yMod1Period = 4; //how many ups and downs there are
    public float yMod1PhaseOffset = 1.0f; //how far the wave is shifted
    public float yMod1Scale = 2.0f; //how big the wave is
    public float yMod1YOffset = 1.1f; //how big the base of the wave is
    public float yMod1TimeResponse = 1.0f; //the amount the wave moves with time

    void Start()
    {
        //we need a mesh filter
        GetComponent<MeshFilter>().mesh = new Mesh();
    }

    // Update is called once per frame
    void Update()
    {
        this.UpdateMesh(GetComponent<MeshFilter>().mesh);
    }

    Mesh UpdateMesh(Mesh m)
    {
        if (m == null)
        {
            m = new Mesh();
        }
        m.Clear();


        Vector3[] vectors = new Vector3[phiDivs * thetaDivs];
        Vector2[] uvs = new Vector2[phiDivs * thetaDivs];
        float radsPerPhiDiv = Mathf.PI / (phiDivs - 1);
        float radsPerThetaDiv = 2.0f * Mathf.PI / thetaDivs;

        float seconds = Time.timeSinceLevelLoad;

        // build an array of vectors holding the vertex data
        int vIndex = 0;
        for (int i = 0; i < phiDivs; i++)
        {
            //  float phi = radsPerPhiDiv * i;
            // u = phi;

            u = Remap(i, 0, phiDivs - 1, 0.5f, 1.0f);

            for (int j = 0; j < thetaDivs; j++)
            {
                //   float theta = radsPerThetaDiv * j;
                // u = phi;
                //v = theta;
                v = Remap(j, 0, thetaDivs, 0, 2 * Mathf.PI);
                //   u = umin + i * (umax - umin) / resolution;
                //  v = vmin + j * (vmax - vmin) / resolution;


                //the get radius function is where 'hamonics' are added
                r = GetRadius(u, v, seconds);

                //add uvs so that we can texture the mesh if we want
                uvs[vIndex] = new Vector2(j * 1.0f / thetaDivs, i * 1.0f / phiDivs);

                //create a vertex 
                //optimization alert: since the only thing that changes here is the radius
                //(when the number of divisions stays the same) we could cache these numbers
                // and use a shader to create and apply the variations in radius and compute 
                // the normals.
                // 
                // x = a cos(u) sin(v)
                // y = a sin(u) sin(v)

                //  z = a(cos(v) + log(tan(v / 2))) + b u

                //   0 <= u, 0 < v

                x = a * Mathf.Cos(u) * Mathf.Sin(v);
                y = a * Mathf.Sin(u) * Mathf.Sin(v);
                z = a * (Mathf.Cos(v) + Mathf.Log(Mathf.Tan(v / 2))) + b * u;



            }
        }
        m.vertices = vectors;
        m.uv = uvs;

        //assign triangles - these take the form of 'triangle strips' wrapping the
        // circumference of the sphere
        //there is room to optimise this by not recalculating/reassigning if the 
        //count of the vertecies hasn't changed because the topology will still
        // be the same.


        int triCount = 2 * (phiDivs - 1) * (thetaDivs);
        int[] triIndecies = new int[triCount * 3];
        int curTriIndex = 0;
        for (int i = 0; i < phiDivs - 1; i++)
        {
            for (int j = 0; j < thetaDivs; j++)
            {
                int ul = i * thetaDivs + j;//"upper left" vert
                int ur = i * thetaDivs + ((j + 1) % thetaDivs);//"upper right" vert
                int ll = (i + 1) * thetaDivs + j;//"lower left" vert
                int lr = (i + 1) * thetaDivs + ((j + 1) % thetaDivs); //"lower right" vert
                                                                      //triangle one
                triIndecies[curTriIndex++] = ul;
                triIndecies[curTriIndex++] = ll;
                triIndecies[curTriIndex++] = ur;

                //triangle two
                triIndecies[curTriIndex++] = ll;
                triIndecies[curTriIndex++] = lr;
                triIndecies[curTriIndex++] = ur;
            }
        }

        m.triangles = triIndecies;
        //use the triangle info to calculate vertex normals so we dont have to B)
        Vector3[] normals = m.normals;
        m.RecalculateNormals();
        return m;

    }

    float GetRadius(float phi, float theta, float time = 0)
    {
        return xMod1YOffset + xMod1Scale * Mathf.Sin(xMod1TimeResponse * time + theta * xMod1Period * time * .1f + xMod1PhaseOffset * time) +
            yMod1YOffset + yMod1Scale * Mathf.Sin(yMod1TimeResponse * time + phi * yMod1Period * time * .1f + yMod1PhaseOffset * time);
    }

    //show a representation in the editor window
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireMesh(UpdateMesh(null), transform.position, transform.rotation, transform.localScale);


    }



    float Remap(float val, float srcMin, float srcMax, float dstMin, float dstMax)
    {
        if (val >= srcMax) return dstMax;
        if (val <= srcMin) return dstMin;
        return dstMin + (val - srcMin) / (srcMax - srcMin) * (dstMax - dstMin);
    }

}



