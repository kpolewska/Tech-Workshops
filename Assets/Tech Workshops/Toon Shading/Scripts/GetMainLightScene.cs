using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GetMainLightScene : MonoBehaviour
{
    public Light MainSceneLight;

    private void Update()
    {
        Shader.SetGlobalColor("_MainSceneLightColor", MainSceneLight.color);
        Shader.SetGlobalVector("_MainSceneLightDirection", MainSceneLight.transform.forward);
    }
}
