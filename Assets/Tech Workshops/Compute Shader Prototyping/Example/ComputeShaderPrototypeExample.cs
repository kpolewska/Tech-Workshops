using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderPrototypeExample : ComputeShaderBlackboardBase
{
    protected override bool ResourceLoadComputeShader => true;
    protected override string ResourceComputeShaderName => "ComputeShaderPrototypeExample";
    protected override string KernelName => "CSMain";
    public override RenderTextureFormat DataTextureFormat { get { return RenderTextureFormat.ARGBHalf; } }

    protected override void DispatchComputeShader()
    {
        //Insert compute shader parameters here
        _compute.SetTexture(_kernel, "DataTex_in", DataTexture());
        _compute.SetTexture(_kernel, "DataTex_out", DataTexture());

        _compute.Dispatch(_kernel, ThreadCountX, ThreadCountY, 1);
    }

    private void LateUpdate()
    {
        DispatchComputeShader();
    }
}