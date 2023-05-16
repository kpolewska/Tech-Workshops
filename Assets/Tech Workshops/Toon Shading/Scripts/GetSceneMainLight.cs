using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GetSceneMainLight : MonoBehaviour
{
    [SerializeField]
    private Light _mainLight;
    public Light MainLight
    {
        get
        {
            return _mainLight;
        }
        private set
        {
            UpdateMainLight(value);
        }
    }

    public void UpdateMainLight(Light newMainLight)
    {
        _mainLight = newMainLight;

        Shader.SetGlobalVector("_MainLightDirection", _mainLight.transform.forward);
        Shader.SetGlobalColor("_MainLightColor", _mainLight.color);
    }

    public void Awake()
    {
        UpdateMainLight(_mainLight);
    }

#if UNITY_EDITOR
    public void Update()
    {
        if (!Application.isPlaying)
        {
            UpdateMainLight(_mainLight);
        }
    }
#endif

    public static GetSceneMainLight Instance { get; protected set; }

    private void OnEnable()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("There is already a GetSceneMainLight component in the scene. Disabling this component.", this);
            enabled = false;
        }
    }

    private void OnDisable()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}
