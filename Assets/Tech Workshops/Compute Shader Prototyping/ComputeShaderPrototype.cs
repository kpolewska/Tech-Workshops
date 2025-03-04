using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderPrototype : ComputeShaderBlackboardBase
{
    protected override bool ResourceLoadComputeShader => false;
    protected override string ResourceComputeShaderName => "ComputeShaderPrototypeExample";
    protected override string KernelName => "CSMain";

    public override RenderTextureFormat DataTextureFormat { get { return RenderTextureFormat.ARGBHalf; } }

    public RenderTexture OutputRenderTexture;

    protected override void DispatchComputeShader()
    {
        //Insert compute shader parameters here
        _compute.SetTexture(_kernel, "DataTex_in", DataTexture());

        _compute.SetTexture(_kernel, "DataTex_out", OutputRenderTexture);

        _compute.Dispatch(_kernel, ThreadCountX, ThreadCountY, ThreadCountZ);
    }

    private void LateUpdate()
    {
        DispatchComputeShader();
    }
}