using UnityEngine;


namespace RoomAliveToolkit
{
    public enum ViewDebugMode
    {
        None, RGB//, Depth
    }
    [AddComponentMenu("RoomAliveToolkit/RATUserViewCamera")]
    /// <summary>
    /// Unity Camera for rendering the user's view in view-dependent projection mapping scenarios.
    /// 
    /// The logic behind the RoomAlive User Views explained:
    /// 
    /// Let's assume that there are at least 4 different layers in the scene that will control what objects are
    /// visible from which camera (user or projector)
    /// 
    /// Create 4 layers in your scene:
    /// - VirtualTextures - for virtual objects that should be texture mapped onto existing surfaces
    /// - Virtual3DObjects - for virtual 3D objects that could be perspective mapped
    /// - StaticSurfaces - for existing static room geometry that is loaded from a file(obj mesh for example)
    /// - DynamicSurfaces- for dynamic depth meshes that represent the physical space
    /// 
    /// In RATProjectionManager set Texture Layers to be "VirtualTextures". These are view independent and therefore do not need to be rendered in user's views. 
    /// 
    /// In this component, select Virtual3DObjects as VirtualObjectMask (layer mask). This will render only the virtual (view-dependent) 3D objects for each user. 
    /// 
    /// However, it is also important to account for real world geometry to correctly occlude the virtual objects. To accomplish that, use RATProjectionPass components and set them up for each
    /// type of real world objects in the scene. The most common situations are for static (OBJ meshes captured during scene calibration) and dynamic objects (Kinect depth meshes)
    /// 
    /// To each RATUserViewCamera add a component RATProjectionPass for each physical layer that you want to projecto on:
    /// select: StaticSurfaces For TargetSurfaceLayer 
    /// Press on "Set Static Defaults" button
    ///
    /// select: DynamicSurfaces For TargetSurfaceLayer
    /// Press on "Set Dynamic Defaults" button
    /// 
    /// </summary>
    public class RATUserViewCamera : MonoBehaviour
    {
        public RATProjectionManager projectionManager;

        [ReadOnly]
        public RenderTexture targetRGBTexture;


        [Space(10)]
        
        [Space(10)]
        public float fieldOfView = 90;
        public float nearClippingPlane = 0.1f;
        public float farClippingPlane = 8f;
        public LayerMask virtualObjectsMask; //select only the layers you want to see in the user's view
          
        [Space(10)]
        public ViewDebugMode debugPlane = ViewDebugMode.RGB;
        /// <summary>
        /// the size of the debug view plane visible in the scene view
        /// </summary>
        [Range(0.1f,3)]
        public float debugPlaneSize = 0.1f; 

        [Space(10)]
        public Color backgroundColor = new Color(0, 0, 0, 0);
        public Color realSurfaceColor = new Color(0, 0, 0, 0);

        public RATProjectionPass[] projectionLayers;

        public Camera viewCamera
        {
            get { return userCam; }
        }

        protected int texWidth = 2048; //width of the off-screen render texture for this user view (needs to be power of 2)
        protected int texHeight = 2048;//height of the off-screen render texture for this user view (needs to be power of 2)

        protected Mesh debugPlaneM;
        protected MeshFilter meshFilter;
        protected MeshRenderer meshRenderer; // meshRenderer for the debug plane
        protected Material meshMat;

        protected int[] indices = new int[] { 0,1,2, 3,2,1};
        protected Vector2[] uv = new Vector2[] { new Vector2(0,1),new Vector2(1,1),new Vector2(0,0),new Vector2(1,0) };
        protected Vector3[] pos = new Vector3[4];

        protected bool initialized = false;
        protected GameObject cameraGO;

        //protected Camera cam;

        public Camera userCam; // expose the userview camera to inspector

        protected Rect rectReadRT;
        protected RATDepthMesh[] depthMeshes;

        public DrawMeshInstancedIndirectDemo boid; // added to render the boids

       // public Camera boidCam;
        public bool hasManager
        {
            get
            {
                return projectionManager != null && projectionManager.isActiveAndEnabled;
            }
        }

        void Awake()
        {
            projectionLayers = gameObject.GetComponents<RATProjectionPass>();

            foreach (RATProjectionPass layer in projectionLayers)
                layer.Init(); // Init() ={ }
        }

        void Start()
        {
            boid = this.gameObject.transform.parent.GetChild(4).GetComponent<DrawMeshInstancedIndirectDemo>();
           // boidCam = Camera.main;

            meshFilter = this.gameObject.AddComponent<MeshFilter>();
            meshRenderer = this.gameObject.AddComponent<MeshRenderer>(); // Add MeshRenderer to this.gameObject, User
            Shader unlitShader = Shader.Find("Unlit/Texture");
            meshMat = new Material(unlitShader); //  meshRenderer.sharedMaterial = meshMat;
                                                 // meshMat.mainTexture = targetRGBTexture;
            debugPlaneM = new Mesh();            // // The debug plane mesh to be added to MeshFilter by   meshFilter.mesh = debugPlaneM;
            meshFilter.hideFlags = HideFlags.HideInInspector;
            meshRenderer.hideFlags = HideFlags.HideInInspector;
            meshMat.hideFlags = HideFlags.HideInInspector;

            if (projectionManager==null)
                projectionManager = GameObject.FindObjectOfType<RATProjectionManager>();
            if(projectionManager!=null)
                projectionManager.RegisterUser(this);

            //Code assumes that this script is added to the camera GO 
            cameraGO = this.gameObject; //camera gameobject is 'USER'

            userCam = this.gameObject.GetComponent<Camera>();

            if (userCam == null)
            { // cam is null in our case; so add a camera component for the userview camera

                userCam = this.gameObject.AddComponent<Camera>(); // this.gameObject, User, will have Camera component
                //Debug.Log($"cam.cullingMask at creation={cam.cullingMask}");
            }

            userCam.hideFlags = HideFlags.HideInInspector;  // | HideFlags.HideInHierarchy

            userCam.rect = new Rect(0, 0, 1, 1);

         
            userCam.enabled = false; //important to disable this camera as we will be calling Render() directly. 

            userCam.aspect = texWidth / texHeight;

            cameraGO.transform.localPosition = new Vector3();

            targetRGBTexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            targetRGBTexture.filterMode = FilterMode.Trilinear;
            targetRGBTexture.autoGenerateMips = true;
            targetRGBTexture.depth = 24;
            targetRGBTexture.Create();

            rectReadRT = new Rect(0, 0, texWidth, texHeight);

            depthMeshes = GameObject.FindObjectsOfType<RATDepthMesh>();

            initialized = true;
        }

        public void Update()
        {
            // this mostly updates the little debug view in the scene editor view
            if (debugPlaneSize < 0) //  // public float debugPlaneSize = 0.1f; 
                debugPlaneSize = 0;

            userCam.nearClipPlane = nearClippingPlane; // cam is the userview camera
            userCam.farClipPlane = farClippingPlane;
            userCam.fieldOfView = fieldOfView;

            

            meshRenderer.enabled = debugPlane != ViewDebugMode.None;
            if (meshRenderer.enabled)
            {
                //meshMat.mainTexture = debugPlane == ViewDebugMode.RGB?targetRGBTexture:targetDepthTexture;
                meshMat.mainTexture = targetRGBTexture; // targetRGBTexture is the result of the userview camera
                                                        // rendering, created by  RenderUserView(); This renderTexture is rendered on the debug plane
                                                        // meshRenderer of that debug plane.
                meshRenderer.sharedMaterial = meshMat; // meshRenderer will use  unlitShader = Shader.Find("Unlit/Texture");

                float z = debugPlaneSize <= nearClippingPlane ? nearClippingPlane : debugPlaneSize;
                float fac = Mathf.Tan(userCam.fieldOfView / 2 / 180f * Mathf.PI);
                float w = z * fac;
                float h = z * fac;
                pos[0] = new Vector3(-w, h, nearClippingPlane);
                pos[1] = new Vector3(w, h, nearClippingPlane);
                pos[2] = new Vector3(-w, -h, nearClippingPlane);
                pos[3] = new Vector3(w, -h, nearClippingPlane);
                debugPlaneM.vertices = pos;
                debugPlaneM.uv = uv;
                debugPlaneM.triangles = indices; // // indices = new int[] { 0,1,2, 3,2,1}: two triangles
                meshFilter.mesh = debugPlaneM;
            }
        } // Update()


        public void LateUpdate()
        {
            if (!initialized)
                return;

            RenderUserView();

            // Projection mapping rendering is actually done by each of the projector cameras
            // Setup things for the last pass which will be rendered from the perspective of the projectors (i.e., Render Pass 3)
            // this "pass" doesn't  do any rendering at this point, but merely sets the correct shaders/materials on all 
            // physical objects in the scene. 
        }

        /// <summary>
        /// Render both virtual and physical objects together from the perspective of the user
        /// </summary>
        /// 

        public void RenderUserView()
        {
            // draw virtual3dObject
            // debug userCam.cullingMask = virtualObjectsMask;

            userCam.backgroundColor = backgroundColor;

            // debug userCam.targetTexture = targetRGBTexture; // off-screen rendering: // The result of rendering with userCam is stored in
                                                      // the renderTexture targetRGBTexture. 

            userCam.clearFlags = CameraClearFlags.SolidColor; //  // clear with the background color
            //when rendering from the perspective of the user, we want the real world to be rendered black 
            //so that the projectors are not re-projecting the textures of the real objects on top of those real objects

            // debug userCam.Render(); // render virtual object by using mesh renderer of each virtual object
                           //(its material and shader(standard shader))
            // end of render of virtual3DObjects

            // debug userCam.clearFlags = CameraClearFlags.Nothing;

            //draw static and dynmaic surfaces
            // debug comment:
            //foreach (RATProjectionPass layer in projectionLayers) // there are two layers: StaticSurface and DynamicSurface
            //{
            //    if (layer.renderUserView && layer.userViewShader != null && layer.enabled)
            //    {
            //        userCam.cullingMask = layer.targetSurfaceLayers; // either StaticSurface or DynamicSurface

            //        Shader.SetGlobalColor("_ReplacementColor", realSurfaceColor);

            //        userCam.RenderWithShader(layer.userViewShader, null);

            //    }
            //}
            // end draw

            //userCam.clearFlags = CameraClearFlags.SolidColor;

            // draw flocking boids
            int GameObjectLayer = LayerMask.NameToLayer("Boid");

           // Debug.Log($"ameObjectLayer = {GameObjectLayer}");

            //int GameObjectLayer = this.gameObject.layer;

           // userCam.enabled = true; // make the camera component enabled so that Graphics.DrawMesh(....camera:cam) work

            userCam.cullingMask = 1 << GameObjectLayer;
            //cam.cullingMask = -1; // all 111....11
            //Debug.Log($"cam.cullingMask={userCam.cullingMask}");

            Graphics.DrawMeshInstancedIndirect(boid.mesh, 0, boid.material, boid.bounds, boid.argsBuffer,
                                                 layer: GameObjectLayer,
                                                // layer: 0,
                                                camera: userCam);


        }//public void RenderUserView()

        // called by userView.RenderProjection(cam) in RATProjector.cs; // render the user view image  to projector camera (cam) by means of 
        // projective texture. 
        public virtual void RenderProjection(Camera projectorCamera) // camera is a projector camera
        {
            // debug comment:

            //RATProjectionPass[] layers = projectionLayers;

            //for (int layerId=0; layerId < layers.Length; layerId++) {
            //    RATProjectionPass layer = layers[layerId];
            //    if (layer == null || !layer.enabled || layer.projectionShader==null || !layer.renderProjectionPass)
            //        continue;
            //    projectorCamera.cullingMask = layer.targetSurfaceLayers; // either StaticSurface or DynamicSurface
            //                                                             // render the targetSurface to the projector camera with
            //                                                             // the render texture image (targetRGBTexture) from the user view;
            //                                                             //The image rendered to the projector camera is the "pre-distorted" image
            //                                                             // to be projected by the real projector.


            //    //todo preload IDs
            //    Shader.SetGlobalVector("_UserViewPos", this.userCam.transform.position); // this.cam is the userview camera

            //    Shader.SetGlobalTexture("_UserViewPointRGB", targetRGBTexture); // targetRGBTexture is the userview Image

            //    //Shader.SetGlobalTexture("_UserViewPointDepth", targetDepthTexture);
            //    Shader.SetGlobalMatrix("_UserVP", this.userCam.projectionMatrix * this.userCam.worldToCameraMatrix);
            //    projectorCamera.RenderWithShader(layer.projectionShader, null);
            //}
        } // RenderProjection
    } // RATUserViewCamera
}//namespace RoomAliveToolkit
