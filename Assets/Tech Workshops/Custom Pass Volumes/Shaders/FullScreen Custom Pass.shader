Shader "FullScreen/FullScreenCustomPass"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    // The PositionInputs struct allow you to retrieve a lot of useful information for your fullScreenShader:
    // struct PositionInputs
    // {
    //     float3 positionWS;  // World space position (could be camera-relative)
    //     float2 positionNDC; // Normalized screen coordinates within the viewport    : [0, 1) (with the half-pixel offset)
    //     uint2  positionSS;  // Screen space pixel coordinates                       : [0, NumPixels)
    //     uint2  tileCoord;   // Screen tile coordinates                              : [0, NumTiles)
    //     float  deviceDepth; // Depth from the depth buffer                          : [0, 1] (typically reversed)
    //     float  linearDepth; // View space Z coordinate                              : [Near, Far]
    // };

    // To sample custom buffers, you have access to these functions:
    // But be careful, on most platforms you can't sample to the bound color buffer. It means that you
    // can't use the SampleCustomColor when the pass color buffer is set to custom (and same for camera the buffer).
    // float4 SampleCustomColor(float2 uv);
    // float4 LoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);

        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        float4 color = float4(0.0, 0.0, 0.0, 0.0);

        // Load the camera color buffer at the mip 0 if we're not at the before rendering injection point
        if (_CustomPassInjectionPoint != CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING)
            color = float4(CustomPassLoadCameraColor(varyings.positionCS.xy, 0), 1);

        // Add your custom pass code here

        // Fade value allow you to increase the strength of the effect while the camera gets closer to the custom pass volume
        float fade = 1 - abs(_FadeValue * 2 - 1);
        float alpha = 1;

        //Grayscale
        float grayscalePixelValue = length(color.rgb) / 3;

        //Depth Feth
        float depthMask = posInput.deviceDepth * 100;
        depthMask = saturate(depthMask);

        //Grayscale effect based on depth!
        //color.rgb = lerp(grayscalePixelValue, color, depthMask);

        //UV 0-1 based color fetch
        //float3 currentPosition = SampleCameraColor(posInput.positionNDC);
        
        //Pixel based color fetch
        float3 currentPixel = LoadCameraColor(posInput.positionSS.xy, 0);
        float3 leftPixel = LoadCameraColor(posInput.positionSS.xy   - float2(1, 0), 0);
        float3 rightPixel = LoadCameraColor(posInput.positionSS.xy  + float2(1, 0), 0);
        float3 bottomPixel = LoadCameraColor(posInput.positionSS.xy - float2(0, 1), 0);
        float3 topPixel = LoadCameraColor(posInput.positionSS.xy    + float2(0, 1), 0);

        //Gradient detection - Useful for sharpening image!
        float2 gradient = float2(length(rightPixel) - length(leftPixel), length(topPixel) - length(bottomPixel));
        float3 sharpen = saturate(gradient.x + gradient.y);

        //Laplacian operation!
        float3 xColorLaplacian = (leftPixel - 2 * currentPixel + rightPixel);
        float3 yColorLaplacian = (bottomPixel - 2 * currentPixel + topPixel);

        float xLaplacian = (length(leftPixel) - 2 * length(currentPixel) + length(rightPixel));
        float yLaplacian = (length(bottomPixel) - 2 * length(currentPixel) + length(topPixel));

        //Laplacian can be used to edge detect!
        float edge = xLaplacian + yLaplacian;
        float edgeStrength = 1;
        float edgeColor = 0;
        float3 edgeOutline = lerp(currentPixel, edgeColor, saturate(edge) * edgeStrength);

        //Low gaussian blur!
        float3 gaussian = (xColorLaplacian + yColorLaplacian) * 0.25;

        //Looks a bit like paint!
        float3 paint = saturate(xColorLaplacian + xColorLaplacian) * 0.25;

        color.rgb += paint;

        return float4(color.rgb + fade, alpha);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
