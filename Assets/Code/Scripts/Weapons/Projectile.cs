using System;
using UnityEngine;

namespace SquareUp.Runtime.Weapons
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour
    {
        public int damage;
        public float speed = 30.0f;
        public float lifetime = 10.0f;
        public float trailPostLifetime = 1.0f;

        private float age;
        private Transform trail;
        
        private Vector2 position;
        private Vector2 velocity;
        private Vector2 force;

        private void Awake()
        {
            trail = transform.Find("Trail");
        }

        private void Start()
        {
            velocity = transform.right * speed;
            position = transform.position;
        }

        private void FixedUpdate()
        {
            Collide();
            Iterate();
        }

        private void Collide()
        {
            var step = velocity.magnitude * Time.deltaTime;
            var direction = velocity.normalized;

            var hit = Physics2D.Raycast(position, direction, step * 1.01f);
            if (!hit) return;

            Destroy();
        }

        private void Iterate()
        {
            position += velocity * Time.deltaTime;
            velocity += force * Time.deltaTime;

            force = Physics.gravity;
            transform.position = position;

            age += Time.deltaTime;
            if (age > lifetime)
            {
                Destroy();
            }
        }

        private void Destroy()
        {
            trail.SetParent(null);
            Destroy(trail.gameObject, trailPostLifetime);
            
            Destroy(gameObject);
        }
    }
}