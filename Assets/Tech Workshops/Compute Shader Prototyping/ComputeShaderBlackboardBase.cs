using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ComputeShaderBlackboardBase : MonoBehaviour
{
    public ComputeShader ComputeShaderToLoad;

    public Vector3Int Resolution = new Vector3Int(1024, 1024, 1);
    public float TexelSize = 1;

    public int MipCount = 0;
    public int AntiAliasingLevel = 1;
    public int AnisotropyLevel = 0;
    public TextureWrapMode WrapMode = TextureWrapMode.Clamp;
    public FilterMode FilteringMode = FilterMode.Point;

    public Transform AnchorRef;
    public Vector3 AnchorPos
    {
        get
        {
            if (AnchorRef)
            {
                return AnchorRef.position;
            }
            else
            {
                return new Vector3(0, 0, 0);
            }
        }
    }

    public bool DebugDrawToSelf = false;
    public bool DebugViewData = true;
    GameObject debugTexture;
    MeshRenderer meshRenderer;

    public abstract RenderTextureFormat DataTextureFormat { get; }

    public virtual RenderTexture DataTexture()
    {
        return _renderTarget;
    }
    protected RenderTexture _renderTarget;

    protected abstract bool ResourceLoadComputeShader { get; }
    protected abstract string ResourceComputeShaderName { get; }
    protected ComputeShader _compute;
    protected abstract string KernelName { get; }
    protected int _kernel;
    protected virtual int ThreadCountX { get { return (Resolution.x + 7) / 8; } }
    protected virtual int ThreadCountY { get { return (Resolution.y + 7) / 8; } }
    protected virtual int ThreadCountZ { get { return (Resolution.z + 7) / 8; } }

    protected virtual void Awake()
    {
        InitData();
        if(DebugViewData) InitDebug();
    }

    protected virtual void InitData()
    {
        if (ResourceLoadComputeShader)
        {
            try
            {
                _compute = Resources.Load<ComputeShader>(ResourceComputeShaderName);
                _kernel = _compute.FindKernel(KernelName);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Cannot find the compute shader in the associated resource folder. Try putting it in a Resource folder next to this script. Error Code: " + e);
                gameObject.SetActive(false);
            }
        }
        else
        {
            _compute = ComputeShaderToLoad;
            _kernel = _compute.FindKernel(KernelName);
        }

        var desc = new RenderTextureDescriptor(Resolution.x, Resolution.y, DataTextureFormat, 0, MipCount);

        if(Resolution.z > 1)
        {
            desc.volumeDepth = Resolution.z;
            desc.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        }

        _renderTarget = new RenderTexture(desc);
        _renderTarget.wrapMode = WrapMode;
        _renderTarget.filterMode = FilteringMode;
        _renderTarget.useMipMap = (MipCount > 0)? true : false;
        _renderTarget.antiAliasing = AntiAliasingLevel;
        _renderTarget.anisoLevel = AnisotropyLevel;
        _renderTarget.name = "CSPrototypeData";

        _renderTarget.enableRandomWrite = true;

        _renderTarget.Create();
    }

    protected virtual void InitDebug()
    {
        if (DebugDrawToSelf)
        {
            try
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshRenderer.material.mainTexture = DataTexture();
            }
            catch(System.Exception e)
            {
                Debug.LogError("Object must have a Mesh renderer and Mesh Filter to draw to self. Error Code: " + e);
            }
        }

        debugTexture = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(debugTexture.GetComponent<Collider>());
        debugTexture.name = (ResourceLoadComputeShader) ? ResourceComputeShaderName : _compute.ToString();

        Vector3 scale = (Vector3)Resolution * TexelSize;
        debugTexture.transform.localScale = scale;
        debugTexture.transform.rotation = Quaternion.Euler(90, 0, 0);
        debugTexture.transform.SetParent(this.transform);
        debugTexture.transform.position = Vector3.zero;

        debugTexture.GetComponent<MeshRenderer>().material.shader = Shader.Find("Unlit/Transparent");
        debugTexture.GetComponent<MeshRenderer>().material.mainTexture = DataTexture();
    }

    protected virtual void DispatchComputeShader()
    {
        DispatchComputeShader(_kernel);
    }

    protected virtual void DispatchComputeShader(int kernelIndex)
    {
        _compute.Dispatch(kernelIndex, ThreadCountX, ThreadCountY, ThreadCountZ);
    }

    private void OnDrawGizmos()
    {
        if(Application.isPlaying)
            DebugView();
    }

    protected virtual void DebugView()
    {
        //Temp change to support 3D tex
        if(_renderTarget.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
        {
            if (debugTexture.activeSelf != DebugViewData)
                debugTexture.SetActive(DebugViewData);
        }

        if (!DebugViewData)
            return;

        transform.position = AnchorPos;
    }
}