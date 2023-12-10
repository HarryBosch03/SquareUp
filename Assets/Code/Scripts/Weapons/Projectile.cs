using System;
using UnityEngine;

namespace SquareUp.Runtime.Weapons
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour
    {
        private const float FallHeight = 0.5f;
        
        public int damage;
        public float speed = 30.0f;
        public float lifetime = 10.0f;
        public float trailPostLifetime = 1.0f;
        public float ghostDistance = 0.5f;
        
        [Space]
        public Transform hitFx;

        private float age;
        private float distanceTraveled;
        private Transform trail;
        private Transform shadow;

        private Vector2 position;
        private Vector2 velocity;
        private Vector2 force;

        private void Awake()
        {
            trail = transform.Find("Trail");
            shadow = transform.Find("Shadow");
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
            
            transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }

        private void Update()
        {
            var x = age / lifetime;
            shadow.position = transform.position + Vector3.down * (1.0f - x * x);
            shadow.localRotation = Quaternion.identity;
        }

        private void Collide()
        {
            var step = velocity.magnitude * Time.deltaTime;
            var direction = velocity.normalized;
            var start = position;

            if (distanceTraveled < ghostDistance)
            {
                if (distanceTraveled + step < ghostDistance) return;
                var shrink = ghostDistance - distanceTraveled;
                start += direction * shrink;
                step -= shrink;
            }

            var hit = Physics2D.Raycast(start, direction, step * 1.01f);
            Debug.DrawRay(start, direction * step, Color.red);
            if (!hit) return;

            Destroy(hit.point, Vector2.Reflect(direction, hit.normal));
        }

        private void Iterate()
        {
            position += velocity * Time.deltaTime;
            velocity += force * Time.deltaTime;

            distanceTraveled += velocity.magnitude * Time.deltaTime;

            force = Vector2.down * 2.0f * FallHeight / (lifetime * lifetime);
            transform.position = position;

            age += Time.deltaTime;
            if (age > lifetime)
            {
                Destroy(position, velocity.normalized);
            }
        }

        private void Destroy(Vector2 point, Vector2 direction)
        {
            if (hitFx)
            {
                Instantiate(hitFx, point, direction.AsRotation());
            }
            
            if (trail)
            {
                trail.SetParent(null);
                Destroy(trail.gameObject, trailPostLifetime);
            }

            Destroy(gameObject);
        }
    }
}