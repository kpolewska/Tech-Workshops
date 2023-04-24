using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleMoveCharacter : MonoBehaviour
{
    public float Speed = 1;

    private Vector3 _inputAxis;
    private CharacterController _characterController;

    // Start is called before the first frame update
    void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        _inputAxis = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        _inputAxis = _inputAxis.normalized * Mathf.Min(_inputAxis.magnitude, 1);

        _characterController.SimpleMove(_inputAxis * Speed);
    }
}