using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SquareUp.Runtime.Player
{
    public class PlayerSpawner : MonoBehaviour
    {
        public GameObject playerPrefab;
        public InputAction joinAction;

        public List<InputDevice> trackedDevices = new();
        
        private void OnEnable()
        {
            joinAction.Enable();
            joinAction.performed += JoinEvent;
        }

        private void OnDisable()
        {
            joinAction.performed -= JoinEvent;
            joinAction.Disable();
        }

        private void JoinEvent(InputAction.CallbackContext ctx)
        {
            var device = ctx.control.device;
            if (trackedDevices.Contains(device)) return;
            trackedDevices.Add(device);
            SpawnPlayerWithDevice(device);
        }

        private void SpawnPlayerWithDevice(InputDevice device)
        {
            var instance = Instantiate(playerPrefab);
            var inputProvider = instance.GetComponent<PlayerInputProvider>();
            if (inputProvider)
            {
                inputProvider.BindInput(device);
            }
        }
    }
}