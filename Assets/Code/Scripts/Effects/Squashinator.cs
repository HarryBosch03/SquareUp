using System;
using UnityEngine;

namespace SquareUp.Runtime
{
    [SelectionBase, DisallowMultipleComponent]
    public sealed class Squashinator : MonoBehaviour
    {
        public Vector2 axis;
        public float spring;
        public float damper;
        public float response;

        private Rigidbody2D body;
        private float position;
        private float velocity;

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody2D>();
        }

        private void Start()
        {
            position = Vector2.Dot(body.position, axis);
            velocity = Vector2.Dot(body.velocity, axis);
        }

        private void FixedUpdate()
        {
            var axis = this.axis.normalized;
            var tPos = Vector2.Dot(body.position, axis);
            var tVel = Vector2.Dot(body.velocity, axis);

            var force = (tPos - position) * spring - velocity * damper;

            position += velocity * Time.deltaTime;
            velocity += force * Time.deltaTime;

            var x = 1.0f + Mathf.Abs(tPos - position) * response;
            var y = 1.0f / x;
            var tangent = new Vector2(-axis.y, axis.x);
            var scale = (Vector3)(axis * x + tangent * y);
            scale.x = Mathf.Abs(scale.x);
            scale.y = Mathf.Abs(scale.y);
            scale.z = 1.0f;
            transform.localScale = scale;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            var axis = this.axis.normalized;
            var position = body.position + axis * (this.position - Vector2.Dot(body.position, axis));
            Gizmos.DrawLine(body.position, position);
            Gizmos.DrawSphere(position, 0.1f);
        }
    }
}
