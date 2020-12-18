using UnityEngine;
using System;

namespace RoomAliveToolkit
{
    //for controlling the rendering loop:
    //http://docs.unity3d.com/Documentation/ScriptReference/Camera.Render.html
    //http://answers.unity3d.com/questions/460596/call-camerarender-twice-rendertexture-contents-dif.html
    //http://docs.unity3d.com/Documentation/ScriptReference/Camera.OnRenderImage.html

    /// <summary>
    /// Behavior that when added to a Unity Camera makes it a projector in the RoomAlive scene
    /// </summary>
    [AddComponentMenu("RoomAliveToolkit/RATProjector")]
    public class RATProjector : MonoBehaviour
    {
        /// <summary>
        /// Asset containing the RoomAlive XML calibration data. 
        /// </summary>
        public RATCalibrationData calibrationData = null;
        /// <summary>
        /// Projector name in the RoomAlive calibration XML file. 
        /// </summary>
        public string nameInConfiguration = "0";

        /// <summary>
        /// A link to the RATProjectionManager
        /// </summary>
        public RATProjectionManager projectionManager;

        public Vector4 lensDist;
        public int imageWidth = 1280;
        public int imageHeight = 800;

        public bool initialized { get; private set; }
        internal Camera projectorCam;

        private ProjectorCameraEnsemble.Projector projConfig;
        private RATDynamicMask dynamicMask;

        public int displayIndex = -1;

        public int userCount
        {
            get
            {
                return projectionManager.userCount;
            }
        }

        public bool hasManager
        {
            get
            {
                return projectionManager != null && projectionManager.isActiveAndEnabled;
            }
        }

        public void Awake()
        {
            projectorCam = this.gameObject.GetComponent<Camera>(); // this.gameObject is Projector_0
            LoadCalibrationData();
            if (projectionManager == null)
            {
                projectionManager = GetComponent<RATProjectionManager>();
                if (projectionManager == null) // // projectionManager is null in our tutorial. Go to the parent myRoom to get it
                    projectionManager = GetComponentInParent<RATProjectionManager>();
                if (projectionManager == null) //  // If the parent does not have it, search in the whole scene
                    projectionManager = GameObject.FindObjectOfType<RATProjectionManager>();
            }
            if (projectionManager != null)
                projectionManager.RegisterProjector(this);

            projectorCam.enabled = false; // set to false in order to use Camera.Render()

            dynamicMask = this.gameObject.GetComponent<RATDynamicMask>(); // get the reference if Projector_0, this.gameObject,
                                                                          // has RATDynamicMask component
                                                                          // RATDynamicMask has OnRenderImage() method; So it is assumed
                                                                          // that this.gameObjectis a Camera gameObject, i.e. a gameObject with Camera omponent.

                                                                          // dynamicMask is null for the first tutorial ?

            initialized = true;
        }

        public void Start()
        {

        }

        public bool hasCalibration
        {
            get
            {
                return calibrationData != null && calibrationData.IsValid();
            }
        }

        internal void LoadCalibrationData()
        {
            projectorCam = this.gameObject.GetComponent<Camera>(); // this.gameObject is Projector_0, which has a Camera component
                                                          // RATProjectionManager has a list of projectors and userviewCameras
            projConfig = null;
            if (hasCalibration)
            {
                ProjectorCameraEnsemble ensembleConfig = calibrationData.GetEnsemble();
                foreach (ProjectorCameraEnsemble.Projector pc in ensembleConfig.projectors)
                {
                    if (pc.name == nameInConfiguration)
                    {
                        projConfig = pc;
                    }
                }
            }
            else
            {
                projConfig = null;
            }


            if (projConfig != null)
            {
                if (displayIndex < 0)
                    displayIndex = projConfig.displayIndex;
                //Debug.Log("Projective Rendering - Loading projector calibration information.");
                imageWidth = projConfig.width;
                imageHeight = projConfig.height;

                //// used by shadow etc...
                //// this is the vertical field of view - fy
                projectorCam.aspect = (float)imageWidth / imageHeight;
                float fieldOfViewRad = 2.0f * (float)Math.Atan((((double)(imageHeight)) / 2.0) / projConfig.cameraMatrix[1, 1]);
                float fieldOfViewDeg = fieldOfViewRad / 3.14159265359f * 180.0f;
                projectorCam.fieldOfView = fieldOfViewDeg;
                Matrix4x4 opencvProjMat = GetProjectionMatrix(projConfig.cameraMatrix, projectorCam.nearClipPlane, projectorCam.farClipPlane);
                projectorCam.projectionMatrix = UnityUtilities.ConvertRHtoLH(opencvProjMat);

                //var irCoef = projConfig.lensDistortion.AsFloatArray();
                //! jolaur -- looks like this is not being used and is now 2 elements instead of four in the new xml format
                //! lensDist = new Vector4(irCoef[0], irCoef[1], irCoef[2], irCoef[3]); 
                lensDist = new Vector4();

                Matrix4x4 worldToLocal = RAT2Unity.Convert(projConfig.pose);
                worldToLocal = UnityUtilities.ConvertRHtoLH(worldToLocal);
                this.transform.localPosition = worldToLocal.ExtractTranslation();
                this.transform.localRotation = worldToLocal.ExtractRotation();
            }
            else
            {
                Debug.Log("Projective Rendering - Using default camera calibration information.");
                lensDist = new Vector4();
            }

        }

        private Matrix4x4 GetProjectionMatrix(RoomAliveToolkit.Matrix intrinsics, float zNear, float zFar)
        {
            float c_x = (float)intrinsics[0, 2];
            float c_y = (float)intrinsics[1, 2];

            //the intrinsics are in Kinect coordinates: X - left, Y - up, Z, forward
            //we need the coordinates to be: X - right, Y - down, Z - forward


            //In the image space x goes right, y goes top to bottom (i.e. down)
            //However in the Kinect camera space x goes left, y goes up and z goes forward (right handed coordinate system).
            //So to perform image lookup, we need to flip x and y. No other reason. 

            c_x = imageWidth - c_x;
            c_y = imageHeight - c_y;

            // http://spottrlabs.blogspot.com/2012/07/opencv-and-opengl-not-always-friends.html
            // http://opencv.willowgarage.com/wiki/Posit
            Matrix4x4 projMat = new Matrix4x4();
            projMat[0, 0] = (float)(2.0 * intrinsics[0, 0] / imageWidth);
            projMat[1, 1] = (float)(2.0 * intrinsics[1, 1] / imageHeight);
            projMat[2, 0] = (float)(-1.0f + 2 * c_x / imageWidth);
            projMat[2, 1] = (float)(-1.0f + 2 * c_y / imageHeight);

            // Note this changed from previous code
            // see here: http://www.songho.ca/opengl/gl_projectionmatrix.html
            projMat[2, 2] = -(zFar + zNear) / (zFar - zNear);
            projMat[3, 2] = -2.0f * zNear * zFar / (zFar - zNear);
            projMat[2, 3] = -1;

            // Transpose tp fit Unity's column major matrix (in contrast to vision raw major ones).
            projMat = projMat.transpose;
            return projMat;
        }

        public void Update()
        {

        }

        public void Render() // called by projector.Render() from OnPostRender() in RATProjectionManager.cs
        {
            if (!hasManager)
                return;

            int prevCulling = projectorCam.cullingMask; // save the culling mask of the projector camera

            bool maskWasEnabled = false;
            if (dynamicMask != null)
            {
                maskWasEnabled = dynamicMask.enabled; // dynamicMask is a monobehavior object
                dynamicMask.enabled = false; // disable the monobehavior object, so that it is not executed at all;
                                             // In particular, its OnImageRender() method will not be called.
            }

            // Render the virtualTexture layer (which is viewer independent) to the projector camera (cam)
            projectorCam.depth = 1;
            projectorCam.backgroundColor = projectionManager.backgroundColor;
            projectorCam.clearFlags = CameraClearFlags.SolidColor; //  Clear with a background color.
            //

            projectorCam.cullingMask = projectionManager.textureLayers; // specified in the inspector

            projectorCam.Render(); // Render the virtual Texture object

            // End:  Render the gameObjects (virtual Objects) whose layer is virtualTexture  to the projector camera (cam).
            // If there are no such gameObjects, no virtualTexture will be drawn in space.
            // VirtualTextures – virtual objects that should be texture mapped onto existing surfaces; 
            // these objects will be rendered as flat user-independent layers, like stickers on the physical geometry
            // To use this feature: Add one plane object, sized appropriately and placed in front of some wall in the scene.
            // Add that plane to VirtualTextures layer. You are supposed to associate a texture image to this plane object,
            // to which MeshRenderer component is attached. 
            
         
            projectorCam.clearFlags = CameraClearFlags.Nothing; //  Don't clear anything; use the current color of the renderTexture result
                                                       //  of the previous rendering
            
            for (int i = 0; i < userCount; i++)
            {
                RATUserViewCamera userView = projectionManager.userViewCameras[i]; // ProjectioinManager has a list of userViewCameras
                if (!userView.isActiveAndEnabled)
                    continue;
                userView.RenderProjection(projectorCam); // render the user view image  to projector camera (cam) by means of 
                                                         // projective texture
                                                         // The image was rendered for userCam from the virtual objects in the scene
                                                         //    RenderUserView() in LateUpdate() in RATUserViewCamera.cs;
                                                         // This method will use this image  as a projective texture for rendering for projectorCam
            }

            if (dynamicMask != null && maskWasEnabled)
            {
                dynamicMask.enabled = maskWasEnabled; // Enable DynamicMask object, so that its OnImageRender() is called
                projectorCam.clearFlags = CameraClearFlags.Nothing;

                projectorCam.cullingMask = 0; // render nothing: 

                projectorCam.Render(); // it is assumed that RATDynamicMask component is attached to cam gameObject, 
                              // so that OnImageRender() method of RATDynamicMaks is invoked upon cam.Render()
                
            }
            

            //Reset / Restore the setting
            projectorCam.cullingMask = prevCulling;
            projectorCam.clearFlags = CameraClearFlags.SolidColor;
        }

        public void RenderTexturesOnly()
        {
            if (!hasManager)
                return;
            projectorCam.backgroundColor = projectionManager.backgroundColor;
            projectorCam.clearFlags = CameraClearFlags.SolidColor;
            projectorCam.cullingMask = projectionManager.textureLayers;
            if (dynamicMask != null)
                dynamicMask.enabled = true;
            //cam.enabled = true;
            projectorCam.Render();
        }

    }
}


