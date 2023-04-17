using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowTrailManager : MonoBehaviour
{
    //Lazy singleton pattern. Used for convenience, but not the best implementation
    private static SnowTrailManager _instance;
    public static SnowTrailManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<SnowTrailManager>();
            }

            return _instance;
        }
    }

    public ComputeShader SnowTrailComputeShader;
    public Vector3Int RenderResolution = new Vector3Int(1024, 1024, 1);

    //This render texture is created within the editor as an asset.
    //A second camera in the scene renders all the objects contained within the Deformation Trail Layer to this texture
    //The result of this render gets added onto the final trail map render texture to keep the deformation between frames
    //The player camera is then set to ignore rendering objects from that layer, so that they aren't visible to player
    public RenderTexture CurrentFrameTrailMapRenderTexture;
    private RenderTexture _finalTrailMapRenderTexture;

    //The trail map will move according to the player / camera position
    public Vector3 SnowTrailCenterPosition = Vector3.zero;

    #region Compute Shader Kernel Parameters

    private string _snowTrailKernelName = "SnowTrailDeformation";
    private int _snowTrailKernelID;
    private string _currentFrameTrailMapPropertyName = "CurrentFrameSnowTrailMap";
    private int _currentFrameTrailMapPropertyID;
    private string _finalTrailMapPropertyName = "FinalSnowTrailMap";
    private int _finalTrailMapPropertyID;
    private string _snowTrailCenterPositionPropertyName = "SnowTrailCenterPosition";
    private int _snowTrailCenterPositionPropertyID;

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

    #endregion

    #region Snow Deformation Trail Textures registration

    #endregion

    void OnEnable()
    {
        //Disable this script if there's no attached Compute Shader
        if (SnowTrailComputeShader == null)
            this.enabled = false;

        //Initialize Compute Shader Kernel ID & Shader Property IDs

        _snowTrailKernelID = SnowTrailComputeShader.FindKernel(_snowTrailKernelName);

        _snowTrailCenterPositionPropertyID = Shader.PropertyToID(_snowTrailCenterPositionPropertyName);

        //Initialize the Render Textures
        RenderTextureInitialization();
    }

    private void OnDisable()
    {
        //Releasing render textures removes them from memory, essentially "destroying" them
        if (_finalTrailMapRenderTexture != null)
            _finalTrailMapRenderTexture.Release();
    }

    private void RenderTextureInitialization()
    {
        _currentFrameTrailMapPropertyID = Shader.PropertyToID(_currentFrameTrailMapPropertyName);
        _finalTrailMapPropertyID = Shader.PropertyToID(_finalTrailMapPropertyName);

        _finalTrailMapRenderTexture = new RenderTexture(CurrentFrameTrailMapRenderTexture);
        _finalTrailMapRenderTexture.enableRandomWrite = true;

        //Until now, all we have done is set the parameters of the render textures, but it doesn't exist on the GPU yet.
        //renderTexture.Create() actually creates the render texture following the parameters set
        _finalTrailMapRenderTexture.Create();

        //Set the final trail map as a global texture, accessible from other shaders, including shadergraph
        Shader.SetGlobalTexture(_finalTrailMapPropertyID, _finalTrailMapRenderTexture);
    }

    void Update()
    {
        UpdatePosition();
        ComputeSnowTrail();
    }

    void UpdatePosition()
    {
        transform.position = SnowTrailCenterPosition;

        //Save the Snow Trail Center Position as a global shader variable, so that we can access it later
        Shader.SetGlobalVector(_snowTrailCenterPositionPropertyID, SnowTrailCenterPosition);
    }

    void ComputeSnowTrail()
    {
        SnowTrailComputeShader.SetTexture(_snowTrailKernelID, _currentFrameTrailMapPropertyID, CurrentFrameTrailMapRenderTexture);
        SnowTrailComputeShader.SetTexture(_snowTrailKernelID, _finalTrailMapPropertyID, _finalTrailMapRenderTexture);

        SnowTrailComputeShader.Dispatch(_snowTrailKernelID, _totalThreadGroups.x, _totalThreadGroups.y, _totalThreadGroups.z);
    }
}
