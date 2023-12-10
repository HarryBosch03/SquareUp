using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace SquareUp.Runtime.Player
{
    public class PlayerInputProvider : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputAsset;
        [SerializeField] private bool lookWithMouse = true;
        [SerializeField] private float gamepadCursorOffset = 5.0f;

        public Camera playerCamera;
        private Dictionary<string, InputActionReference> actions = new();

        public Vector2 LookPosition { get; private set; }
        public Vector2 LookDirection { get; private set; }

        public void BindInput(InputDevice device)
        {
            lookWithMouse = device is Keyboard;
            if (lookWithMouse)
            {
                inputAsset.devices = new InputDevice[] { device, Mouse.current };
            }
            else
            {
                inputAsset.devices = new InputDevice[] { device };
            }
        }

        public InputAction this[string name] => actions[name];

        private void Awake()
        {
            inputAsset = Instantiate(inputAsset);
            inputAsset.Enable();
            inputAsset.devices = new ReadOnlyArray<InputDevice>();

            foreach (var action in inputAsset.FindActionMap("Player"))
            {
                actions.Add(action.name, InputActionReference.Create(action));
            }

            playerCamera = Camera.main;
        }

        private void OnDestroy() { Destroy(inputAsset); }

        private void Update() { GetLookInput(); }

        private void GetLookInput()
        {
            var center = (Vector2)transform.position;
            if (lookWithMouse)
            {
                LookPosition = GetMousePosition();
                LookDirection = (LookPosition - center).normalized;
            }
            else
            {
                var input = Vector("Look");
                if (input.magnitude > 0.2f) LookDirection = input;

                LookPosition = LookDirection * gamepadCursorOffset + center;
            }
        }

        private Vector2 GetMousePosition() => playerCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        public T Read<T>(string name) where T : struct
        {
            var actionRef = actions[name];
            return actionRef.action?.ReadValue<T>() ?? default;
        }

        public bool Pressed(string name) => Axis(name) > 0.5f;
        public float Axis(string name) => Read<float>(name);
        public Vector2 Vector(string name) => Read<Vector2>(name);
    }
}