using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawMeshInstancedIndirectDemo : MonoBehaviour {

   // Camera userViewCamera;
   // public int boidLayer = 12;

    public int population; 
    public float range;

    public float deltaTime;
    public float neighborRadius;
    public float minTheta;

    [Header("Cohesion")]
    public float cohSpeed;
    public float cohWeight;

    [Header("Alignment")]
    public float alignWeight;

    [Header("Avoidance")]
    public float avoidWeight;
    public float avoidanceRadius;
    public float minDistance;

    //[Header("Attract")]
    //public float attractSpeed;
    //public float attractWeight;
    //public float distanceFromAttracter;

    Vector3 boundsSize;

    public Material material;
    public ComputeShader computeShader; 
    // public Transform pusher;

    public ComputeBuffer meshPropertiesBuffer;
    public ComputeBuffer argsBuffer;

    public Mesh mesh; 
    public Bounds bounds;

    //Vector3[] currentVelocityArray;
    //Vector3[] cohVelocityArray;
    //Vector3[] alignVelocityArray;
    //Vector3[] avoidVelocityArray;

    //private ComputeBuffer currentVelocityBuffer;
    //private ComputeBuffer cohVelocityBuffer;
    //private ComputeBuffer alignVelocityBuffer;
    //private ComputeBuffer avoidVelocityBuffer;

    int kernel;
    MeshProperties[] properties;
    private struct MeshProperties { 
        public Matrix4x4 mat;
        public Vector4 color;
        public Vector3 velocity;

        public static int Size() {
            return
                sizeof(float) * 4 * 4 + // mat
                sizeof(float) * 4 +     // color
                sizeof(float) * 3;      // velocity 
        }
    }
    private void Setup() {
        bounds = new Bounds(this.gameObject.transform.position, Vector3.one * (range + 1));
        boundsSize = Vector3.one * (range + 1);
        InitializeBuffers(); 
    }

    private void InitializeBuffers() { 
        kernel = computeShader.FindKernel("CSMain"); 

        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)population;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments); 
        argsBuffer.SetData(args);
        //saturationArray ={0,9,0,6,0,8,0.8,0.9}
        // Initialize buffer with the given population.
        properties = new MeshProperties[population]; 
        // float satArray[] = [0.]
        for (int i = 0; i < population; i++)
        {
            if (i < 50)
            {
                MeshProperties props = new MeshProperties();
                Vector3 position = new Vector3(Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f));
                Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
                Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f);
                props.mat = Matrix4x4.TRS(position, rotation, scale);
                props.color = Color.HSVToRGB(i * 0.02f, 1.0f, 1.0f);

                props.velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));

                properties[i] = props;
            }
            else
            {
                MeshProperties props = new MeshProperties();
                Vector3 position = new Vector3(Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f), Random.Range(-range / 2.0f, range / 2.0f));
                Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
                Vector3 scale = new Vector3(0.1f, 0.1f, 0.1f);

                props.mat = Matrix4x4.TRS(position, rotation, scale);
                props.color = Color.HSVToRGB(Random.Range(0.65f, 0.8f), Random.Range(0.4f, 0.6f), 1);

                props.velocity = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));

                properties[i] = props;
            }
        }

        meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);
        computeShader.SetBuffer(kernel, "_Properties", meshPropertiesBuffer); // for compute shader. 
        material.SetBuffer("_Properties", meshPropertiesBuffer); // for regular shader.

        //currentVelocityArray = new Vector3[population];
        //currentVelocityBuffer = new ComputeBuffer(population, 12);
        //currentVelocityBuffer.SetData(currentVelocityArray);
        //computeShader.SetBuffer(kernel, "_currentVelocityBuffer", currentVelocityBuffer);

        //cohVelocityArray = new Vector3[population];
        //cohVelocityBuffer = new ComputeBuffer(population, 12);
        //cohVelocityBuffer.SetData(cohVelocityArray);
        //computeShader.SetBuffer(kernel, "_cohVelocityBuffer", cohVelocityBuffer);

        //alignVelocityArray = new Vector3[population];
        //alignVelocityBuffer = new ComputeBuffer(population, 12);
        //alignVelocityBuffer.SetData(alignVelocityArray);
        //computeShader.SetBuffer(kernel, "_alignVelocityBuffer", alignVelocityBuffer);

        //avoidVelocityArray = new Vector3[population];
        //avoidVelocityBuffer = new ComputeBuffer(population, 12);
        //avoidVelocityBuffer.SetData(avoidVelocityArray);
        //computeShader.SetBuffer(kernel, "_avoidVelocityBuffer", avoidVelocityBuffer);
    }

    private void Start() {
        // get the camera 
       // userViewCamera = this.gameObject.GetComponentInParent<Camera>();
        Setup();
    }

    private void Update() { 
        // computeShader.SetVector("_PusherPosition", pusher.position); 

        // computeShader.SetFloat("_distanceFromAttracter", distanceFromAttracter);
        computeShader.SetFloat("_neighborRadius", neighborRadius);
        computeShader.SetFloat("_deltaTime", deltaTime);
        computeShader.SetInt("_population", population); 
        computeShader.SetFloat("_avoidanceRadius", avoidanceRadius);
        computeShader.SetFloat("_cohSpeed", cohSpeed);
        // computeShader.SetFloat("_attractSpeed", attractSpeed);
        computeShader.SetFloat("_cohWeight", cohWeight);
        computeShader.SetFloat("_alignWeight", alignWeight);
        computeShader.SetFloat("_avoidWeight", avoidWeight);
        computeShader.SetFloat("_minDistance", minDistance);
        computeShader.SetFloat("_minTheta", minTheta);
        computeShader.SetVector("_boundSize", boundsSize);

        computeShader.Dispatch(kernel, Mathf.CeilToInt(population / 64f), 1, 1);

        //currentVelocityBuffer.GetData(currentVelocityArray); 
        //cohVelocityBuffer.GetData(cohVelocityArray); // cohesion velocity buffer
        //alignVelocityBuffer.GetData(alignVelocityArray); // alignment velocity buffer
        //avoidVelocityBuffer.GetData(avoidVelocityArray); // avoidance velocity buffer

        //for (int i = 0; i < population; i++)
        //{
        //    //Debug.Log($"currentVelocity[{i}] = {currentVelocityArray[i]}");
        //    //Debug.Log($"cohVelocity[{i}] = {cohVelocityArray[i]}");
        //    //Debug.Log($"alignVelocity[{i}] = {alignVelocityArray[i]}");
        //    //Debug.Log($"avoidVelocity[{i}] = {avoidVelocityArray[i]}");
        //}

        //Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
        //Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer, layer : 0, camera : Camera.main) ;

    }

    private void OnDisable()
    {
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }

    private void FixedUpdate()
    {
        // Debug.Log(Time.deltaTime);
    }
}