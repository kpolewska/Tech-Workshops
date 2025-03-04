using UnityEngine;

public class SnowTrailManager : MonoBehaviour
{
    // Lazy singleton pattern. Used for convenience, but not the best implementation
    private static SnowTrailManager _instance;
    public static SnowTrailManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindFirstObjectByType<SnowTrailManager>();
            }

            return _instance;
        }
    }

    public ComputeShader SnowTrailComputeShader;

    public Camera TrailRenderCamera;
    public float SnowTrailWorldspaceScale = 100;
    public LayerMask TrailRenderLayerMask;

    // This render texture is created within the editor as an asset.
    // A second camera in the scene renders all the objects contained within the Deformation Trail Layer to this texture
    // The result of this render gets added onto the final trail map render texture to keep the deformation between frames
    // The player camera is then set to ignore rendering objects from that layer, so that they aren't visible to player
    public RenderTexture CurrentFrameTrailMapRenderTexture;
    private RenderTexture _compositeTrailMapRenderTexture;
    private RenderTexture _finalTrailMapRenderTexture;

    // The trail map will move according to the player / camera position.
    private Vector3 _snowTrailCenterPosition = Vector3.zero;
    private Vector3 _relativePositionDifference = Vector3.zero;

    #region Compute Shader Kernel Parameters

    private string _snowTrailKernelName = "SnowTrailDeformation";
    private int _snowTrailKernelID;

    // The resolution of the render texture and the thread size of the compute shader kernel needs to
    // be stored to calculate the total amount of thread groups. Total # groups = resolution / threadSize
    private Vector3Int RenderResolution = new Vector3Int(1024, 1024, 1);
    private Vector3Int _threadsPerGroup = new Vector3Int(8, 8, 1);
    
    private Vector3Int _totalThreadGroups
    {
        get
        {
            return new Vector3Int
                (
                    RenderResolution.x / _threadsPerGroup.x,
                    RenderResolution.y / _threadsPerGroup.y,
                    RenderResolution.z / _threadsPerGroup.z
                );
        }
    }

    // The name of the variables used in the Compute shaders as strings.
    // Shader.PropertyToID is then used to transform it to its actual identifier, stored under the [...]PropertyID variables below.
    private string _currentFrameTrailMapPropertyName = "CurrentFrameSnowTrailMap"; //The current frame deformation trail render
    private string _compositeFrameTrailPropertyName = "CompositeFrameSnowTrailMap"; //The previous frame result trail render
    private string _finalTrailMapPropertyName = "FinalSnowTrailMap"; //The new result trail render
    private string _snowTrailWorldspaceScalePropertyName = "SnowTrailWorldspaceScale"; //The scale of the snow trail in worldspace units.
    private string _snowTrailCenterPositionPropertyName = "SnowTrailCenterPosition";
    // Instead of sending the center position of the render texture, we send the relative position difference between current frame and last frame
    // This is done to ensure objects are properly drawn to the map.
    private string _relativePositionDifferencePropertyName = "RelativePositionDifference";
    
    private int _currentFrameTrailMapPropertyID;
    private int _compositeFrameTrailMapPropertyID;
    private int _finalTrailMapPropertyID;
    private int _snowTrailWorldspaceScalePropertyID;
    private int _snowTrailCenterPositionPropertyID;
    private int _relativePositionDifferencePropertyID;

    #endregion

    private void OnEnable()
    {
        // Disable this script if there's no attached Compute Shader or if the compute kernel cannot be found
        if (SnowTrailComputeShader == null || SnowTrailComputeShader.HasKernel(_snowTrailKernelName) == false)
        {
            this.enabled = false;
        }

        // Find the compute kernel and get its index
        _snowTrailKernelID = SnowTrailComputeShader.FindKernel(_snowTrailKernelName);

        //Get the exact Thread Group size directly from the Compute Shader kernel
        uint xThread, yThread, zThread;
        SnowTrailComputeShader.GetKernelThreadGroupSizes(_snowTrailKernelID, out xThread, out yThread, out zThread);
        _threadsPerGroup = new Vector3Int((int)xThread, (int)yThread, (int)zThread);

        SetupRenderCamera();

        SetShaderProperties();

        RenderTextureInitialization();
    }

    private void OnDisable()
    {
        //Releasing render textures removes them from memory, essentially "destroying" them
        if (_finalTrailMapRenderTexture != null)
            _finalTrailMapRenderTexture.Release();
    }

    private void SetupRenderCamera()
    {
        //CurrentFrameTrailRenderCamera = transform.GetComponentInChildren(typeof(Camera))
        TrailRenderCamera.transform.position = new Vector3(0, SnowTrailWorldspaceScale, 0);

        TrailRenderCamera.orthographic = true;
        TrailRenderCamera.orthographicSize = SnowTrailWorldspaceScale / 2;
        TrailRenderCamera.farClipPlane = SnowTrailWorldspaceScale + 1000;
        TrailRenderCamera.cullingMask = TrailRenderLayerMask;
        TrailRenderCamera.useOcclusionCulling = false;
        TrailRenderCamera.backgroundColor = Color.black;
        TrailRenderCamera.targetTexture = CurrentFrameTrailMapRenderTexture;
    }

    // Find the actual Shader variables IDs using their Property Names
    private void SetShaderProperties()
    {
        _currentFrameTrailMapPropertyID = Shader.PropertyToID(_currentFrameTrailMapPropertyName);
        _compositeFrameTrailMapPropertyID = Shader.PropertyToID(_compositeFrameTrailPropertyName);
        _finalTrailMapPropertyID = Shader.PropertyToID(_finalTrailMapPropertyName);

        _snowTrailWorldspaceScalePropertyID = Shader.PropertyToID(_snowTrailWorldspaceScalePropertyName);
        _snowTrailCenterPositionPropertyID = Shader.PropertyToID(_snowTrailCenterPositionPropertyName);
        _relativePositionDifferencePropertyID = Shader.PropertyToID(_relativePositionDifferencePropertyName);
    }

    // Initialize the Render Textures by copying the current frame render texture format onto the final map
    private void RenderTextureInitialization()
    {
        //Create a new render texture copying the current frame render texture format
        _compositeTrailMapRenderTexture = new RenderTexture(CurrentFrameTrailMapRenderTexture);
        _finalTrailMapRenderTexture = new RenderTexture(CurrentFrameTrailMapRenderTexture);
        //We must be able to write onto this texture, so we need to enable Random Write
        _finalTrailMapRenderTexture.enableRandomWrite = true;

        // Until now, all we have done is set the parameters of the render textures, but it doesn't exist on the GPU yet.
        // renderTexture.Create() actually creates the render texture following the parameters set
        _compositeTrailMapRenderTexture.Create();
        _finalTrailMapRenderTexture.Create();

        //Set the final trail map as a global texture, accessible from other shaders, including shadergraph
        Shader.SetGlobalTexture(_finalTrailMapPropertyID, _finalTrailMapRenderTexture);
    }

    //We update at the end of the frame to ensure all position and render texture changes are taken into account
    private void LateUpdate()
    {
        UpdatePosition();
        ComputeSnowTrail();
    }

    //Update the center position of the snow trail & the relative position difference between this frame and previous frame
    private void UpdatePosition()
    {
        // Save the Snow Trail Center Position as a global shader variable, so that we can access it in the Deformable Snow shadergraph
        Shader.SetGlobalVector(_snowTrailCenterPositionPropertyID, _snowTrailCenterPosition);

        // Update the relative position difference before actually moving. (NewPos - CurrentPos)
        // This value is used to readjust the trail objects in the compute shader.
        // Please refer to the SnowTrailDeformation compute shader for more info!
        _relativePositionDifference = _snowTrailCenterPosition - transform.position;

        //Move the snow trail to the new center position
        transform.position = _snowTrailCenterPosition;
    }

    private void ComputeSnowTrail()
    {
        //Copy the results from last frame onto the composite trail render texture
        //Essentially, we cannot read and write to a texture using SampleLevel.
        //Because of this, we need to copy the final trail texture. One used for reading, the other for writing.
        Graphics.CopyTexture(_finalTrailMapRenderTexture, _compositeTrailMapRenderTexture);

        SnowTrailComputeShader.SetTexture(_snowTrailKernelID, _currentFrameTrailMapPropertyID, CurrentFrameTrailMapRenderTexture);
        SnowTrailComputeShader.SetTexture(_snowTrailKernelID, _compositeFrameTrailMapPropertyID, _compositeTrailMapRenderTexture);
        SnowTrailComputeShader.SetTexture(_snowTrailKernelID, _finalTrailMapPropertyID, _finalTrailMapRenderTexture);

        SnowTrailComputeShader.SetFloat(_snowTrailWorldspaceScalePropertyID, SnowTrailWorldspaceScale);
        SnowTrailComputeShader.SetVector(_relativePositionDifferencePropertyID, _relativePositionDifference);
        Debug.Log(_relativePositionDifference);

        SnowTrailComputeShader.Dispatch(_snowTrailKernelID, _totalThreadGroups.x, _totalThreadGroups.y, _totalThreadGroups.z);
    }

    public void SetCenterPosition(Vector3 newCenterPosition)
    {
        _snowTrailCenterPosition = newCenterPosition;
    }
}
