using System.Collections.Generic;
using UnityEngine;

namespace SquareUp.Runtime.Player
{
    public class CameraController : MonoBehaviour
    {
        public float minOrthographicSize = 5.0f;
        public float border = 15.0f;

        public float damping = 0.3f;

        private Vector2 minPosition;
        private Vector2 maxPosition;
        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
        }

        private void FixedUpdate()
        {
            if (PlayerController.All.Count == 0) return;
            
            var min = Vector2.one * float.MaxValue;
            var max = Vector2.one * float.MinValue;
            foreach (var player in PlayerController.All)
            {
                var p = (Vector2)player.transform.position;

                min.x = Mathf.Min(min.x, p.x);
                min.y = Mathf.Min(min.y, p.y);
                
                max.x = Mathf.Max(max.x, p.x);
                max.y = Mathf.Max(max.y, p.y);
            }

            maxPosition = Vector2.Lerp(maxPosition, max, Time.deltaTime / Mathf.Max(Time.unscaledDeltaTime, damping));
            minPosition = Vector2.Lerp(minPosition, min, Time.deltaTime / Mathf.Max(Time.unscaledDeltaTime, damping));
            
            var center = (maxPosition + minPosition) * 0.5f;
            var size = maxPosition - minPosition;
            
            size += Vector2.one * border;

            cam.transform.position = new Vector3(center.x, center.y, -10.0f);
            cam.orthographicSize = Mathf.Max(size.x / cam.aspect, size.y) * 0.5f;
            cam.orthographicSize = Mathf.Max(cam.orthographicSize, minOrthographicSize);
        }
    }
}