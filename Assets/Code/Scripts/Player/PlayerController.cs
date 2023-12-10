using System;
using System.Collections.Generic;
using SquareUp.Runtime.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SquareUp.Runtime.Player
{
    [SelectionBase, DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Space]
        public float moveSpeed = 10.0f;
        public float accelerationTime = 0.1f;

        [Space]
        public Projectile projectilePrefab;
        public float fireDelay = 0.4f;
        public float singleFireDelayThreshold = 0.2f;
        public int projectilesPerShot = 1;
        public float spray = 2.0f;
        public int maxAmmo = 3;
        public int currentAmmo;
        public float reloadTime = 1.0f;

        [Space]
        [Range(0.0f, 1.0f)]
        public float animationSmoothing;

        private Rigidbody2D body;
        private PlayerInputProvider input;

        private bool shootFlag;
        
        private float rotation;
        private Transform visuals;
        private Action<Color> setColor;

        private Arm leftArm, rightArm;
        private Arm primaryArm, secondaryArm;

        private float lastFireTime;

        public static readonly List<PlayerController> All = new();

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            
            input = GetComponent<PlayerInputProvider>();

            visuals = transform.Find("Visuals");

            leftArm = new Arm(transform, 'L');
            rightArm = new Arm(transform, 'R');

            SetupColorFunc();
            setColor(Color.HSVToRGB(Random.Range(0, 16) / 16.0f, 0.8f, 0.8f));
            
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = 7;
            }
        }

        private void SetupColorFunc()
        {
            var body = visuals.Find<SpriteRenderer>("Body");
            var bodyBaseColor = body.color;

            var armBaseColorL = leftArm.arm.startColor;
            var armBaseColorR = rightArm.arm.startColor;
            
            setColor = color =>
            {
                body.color = bodyBaseColor * color;
                
                leftArm.arm.startColor = armBaseColorL * color;
                leftArm.arm.endColor = leftArm.arm.startColor;
                
                rightArm.arm.startColor = armBaseColorR * color;
                rightArm.arm.endColor = rightArm.arm.startColor;
            };
        }

        private void OnEnable() { All.Add(this); }

        private void OnDisable() { All.Remove(this); }

        private void FixedUpdate()
        {
            primaryArm = input.LookDirection.x > 0.0f ? rightArm : leftArm;
            secondaryArm = input.LookDirection.x > 0.0f ? leftArm : rightArm;

            primaryArm.Update(input.LookPosition);
            secondaryArm.Update((Vector2)transform.position + new Vector2(input.LookDirection.x > 0.0f ? -1 : 1, 0.0f));

            Move();
            Shoot();
            Reload();

            rotation = Mathf.Lerp(input.LookDirection.x > 0.0f ? 1 : -1, rotation, animationSmoothing);

            shootFlag = false;
        }

        private void Reload()
        {
            if (currentAmmo >= maxAmmo) return;
            if (Time.time - lastFireTime < reloadTime) return;

            currentAmmo = maxAmmo;
        }

        private void Shoot()
        {
            if (fireDelay > singleFireDelayThreshold)
            {
                if (!shootFlag) return;
            }
            else
            {
                if (!input.Pressed("Shoot")) return;
            }

            if (Time.time - lastFireTime < fireDelay) return;
            if (currentAmmo == 0) return;

            for (var i = 0; i < projectilesPerShot; i++)
            {
                var position = (Vector2)primaryArm.hand.position;
                var rotation = (input.LookPosition - position).ToDeg() + Random.Range(-spray, spray);
                Instantiate(projectilePrefab, position, rotation.AsRotation());
            }

            lastFireTime = Time.time;
            currentAmmo--;
        }

        private void Update()
        {
            if (input["Shoot"].WasPerformedThisFrame()) shootFlag = true;
        }

        private void LateUpdate() { Animate(); }

        private void Animate() { visuals.rotation = Quaternion.Euler(0.0f, rotation * 90.0f - 90.0f, 0.0f); }
        
        private void Move()
        {
            var input = this.input.Vector("Move");
            var target = input * moveSpeed;
            var acceleration = 2.0f / accelerationTime;

            var force = Vector2.ClampMagnitude(target - body.velocity, moveSpeed) * acceleration;
            body.velocity += force * Time.deltaTime;
        }
    }
}