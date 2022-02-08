using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

namespace Game.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class Movement : MonoBehaviour
    {
        
        [HideInInspector,SerializeField] private Rigidbody rb;

        [SerializeField] private InputActionReference moveInput;
        [SerializeField] private InputActionReference lookInput;

        private Vector2 _move, _look;

        private void Reset()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            moveInput.action.Enable();
            lookInput.action.Enable();
        }

        private void Update()
        {
            _move = moveInput.action.ReadValue<Vector2>();
            _look = lookInput.action.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            rb.AddForce(_move, ForceMode.VelocityChange);
        }
    }
}
