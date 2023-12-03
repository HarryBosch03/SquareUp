using System;
using System.Collections.Generic;
using SquareUp.Runtime.Weapons;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace SquareUp.Runtime.Player
{
    [SelectionBase, DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        public InputActionAsset inputAsset;
        public bool lookWithMouse = true;
        public float gamepadCursorOffset = 5.0f;

        [Space]
        public float moveSpeed = 10.0f;
        public float accelerationTime = 0.1f;
        [Range(0.0f, 1.0f)]
        public float airMovementPenalty = 0.9f;
        public float stepHeight = 0.51f;

        [Space]
        public float jumpHeight = 2.0f;
        public float wallJumpForce = 5.0f;
        public float wallSlideForce = 5.0f;
        public float upGravity = 2.0f;
        public float downGravity = 3.0f;

        [Space]
        public Vector2 collisionOffset = new(0.0f, 0.5f);
        public Vector2 collisionSize = new(0.5f, 1.0f);
        public LayerMask collisionMask = ~0;

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

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction shootAction;

        private bool jumpFlag;
        private bool shootFlag;
        private bool isOnGround;
        private RaycastHit2D groundHit;
        private bool isOnWall;
        private RaycastHit2D wallHit;

        private float rotation;
        private Transform visuals;
        private Transform center;
        private Action<Color> setColor;

        private Arm leftArm, rightArm;
        private Arm primaryArm, secondaryArm;

        private Vector2 lookPosition;
        private Vector2 lookDirection;
        private Camera mainCam;

        private float lastFireTime;

        public static readonly List<PlayerController> All = new();

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            mainCam = Camera.main;

            visuals = transform.Find("Visuals");
            center = transform.Find("Center");

            leftArm = new Arm(transform, 'L');
            rightArm = new Arm(transform, 'R');

            inputAsset = Instantiate(inputAsset);
            inputAsset.Enable();
            moveAction = inputAsset.FindAction("Move");
            lookAction = inputAsset.FindAction("Look");
            jumpAction = inputAsset.FindAction("Jump");
            shootAction = inputAsset.FindAction("Shoot");

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

        public void BindInput(InputDevice device)
        {
            inputAsset.devices = new InputDevice[] { device };
            lookWithMouse = device is Keyboard;
        }

        private void FixedUpdate()
        {
            primaryArm = lookDirection.x > 0.0f ? rightArm : leftArm;
            secondaryArm = lookDirection.x > 0.0f ? leftArm : rightArm;

            primaryArm.Update(lookPosition);
            secondaryArm.Update((Vector2)center.position + new Vector2(lookDirection.x > 0.0f ? -1 : 1, 0.0f));

            Move();
            Jump();
            Shoot();
            WallSlide();
            Collide();
            SetGravity();
            Reload();

            rotation = Mathf.Lerp(lookDirection.x > 0.0f ? 1 : -1, rotation, animationSmoothing);

            jumpFlag = false;
            shootFlag = false;
        }

        private void Reload()
        {
            if (currentAmmo >= maxAmmo) return;
            if (Time.time - lastFireTime < reloadTime) return;

            currentAmmo = maxAmmo;
        }

        private void WallSlide()
        {
            var input = moveAction.ReadValue<Vector2>().x;
            if (isOnWall && input * wallHit.normal.x < 0.0f && Mathf.Abs(input) > 0.5f && body.velocity.y < 0.0f)
            {
                var force = sqr(Mathf.Max(0.0f, -body.velocity.y)) * wallSlideForce;
                force = Mathf.Min(force, -body.velocity.y / Time.deltaTime);
                body.AddForce(Vector2.up * force);
            }

            float sqr(float x) => x * x;
        }

        private void Shoot()
        {
            if (fireDelay > singleFireDelayThreshold)
            {
                if (!shootFlag) return;
            }
            else
            {
                if (!shootAction.IsPressed()) return;
            }

            if (Time.time - lastFireTime < fireDelay) return;
            if (currentAmmo == 0) return;

            for (var i = 0; i < projectilesPerShot; i++)
            {
                var position = (Vector2)primaryArm.hand.position;
                var rotation = (lookPosition - position).ToDeg() + Random.Range(-spray, spray);
                Instantiate(projectilePrefab, position, rotation.AsRotation());
            }

            lastFireTime = Time.time;
            currentAmmo--;
        }

        private void SetGravity() { body.gravityScale = body.velocity.y > 0.0f ? upGravity : downGravity; }

        private void Update()
        {
            if (jumpAction.WasPerformedThisFrame()) jumpFlag = true;
            if (shootAction.WasPerformedThisFrame()) shootFlag = true;

            var center = (Vector2)this.center.position;
            if (lookWithMouse)
            {
                lookPosition = GetMousePosition();
                lookDirection = (lookPosition - center).normalized;
            }
            else
            {
                var input = lookAction.ReadValue<Vector2>();
                if (input.magnitude > 0.2f) lookDirection = input;

                lookPosition = lookDirection * gamepadCursorOffset + center;
            }
        }

        private void LateUpdate() { Animate(); }

        private void Animate() { visuals.rotation = Quaternion.Euler(0.0f, rotation * 90.0f - 90.0f, 0.0f); }

        private Vector2 GetMousePosition() => mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        private void Move()
        {
            var input = moveAction.ReadValue<Vector2>();
            var target = input.x * moveSpeed;
            var acceleration = 2.0f / accelerationTime;

            float force;
            if (isOnGround)
            {
                force = Mathf.Clamp(target - body.velocity.x, -moveSpeed, moveSpeed) * acceleration;
            }
            else
            {
                force = Mathf.Clamp(target - body.velocity.x, -moveSpeed, moveSpeed) * acceleration * (1.0f - airMovementPenalty) * Mathf.Abs(input.x);
            }

            body.velocity += Vector2.right * force * Time.deltaTime;
        }

        private void Jump()
        {
            if (!jumpFlag) return;

            var gravity = -Physics2D.gravity.y * upGravity;
            var force = Vector2.up * (Mathf.Sqrt(2.0f * jumpHeight * gravity) - body.velocity.y);

            if (isOnGround)
            {
                body.velocity += force;
            }
            else if (isOnWall)
            {
                force += Vector2.right * (wallHit.normal.x > 0.0f ? 1 : -1) * wallJumpForce;
                body.velocity += force;
            }
        }

        private void Collide()
        {
            CheckForWalls();
            CheckForGround();
        }

        private void CheckForWalls()
        {
            var offset = collisionOffset;
            var size = collisionSize;

            size.y -= stepHeight;
            offset.y += stepHeight * 0.5f;

            Collide(Vector2.up, new Vector2(0.0f, offset.y + size.y * 0.5f - 0.25f), Vector2.zero, 0.25f, 1);

            isOnWall = false;
            CheckForWall(1);
            CheckForWall(-1);
        }

        private void CheckForWall(int direction)
        {
            var iterations = 16;
            for (var i = 0; i < iterations; i++)
            {
                var position = body.position;
                var offset = Vector2.up * ((i / (iterations - 1.0f) - 0.5f) * (collisionSize.y - stepHeight) + collisionOffset.y + stepHeight * 0.5f);

                var castDistance = collisionSize.x * 0.5f;
                var hit = Physics2D.Raycast(position + offset, Vector2.right * direction, castDistance, collisionMask);
                if (!hit) continue;

                wallHit = hit;
                isOnWall = true;
                body.position += Vector2.right * (castDistance - hit.distance) * -direction;
                body.velocity += Vector2.right * direction * Mathf.Min(0.0f, -body.velocity.x * direction);
            }
        }


        private void Collide(Vector2 direction, Vector2 offset, Vector2 spread, float distance, int iterations = 16)
        {
            for (var i = 0; i < iterations; i++)
            {
                var position = body.position;
                var localOffset = spread * ((iterations > 1 ? i / (iterations - 1.0f) : 0.5f) - 0.5f);

                var start = position + offset + localOffset;
                var hit = Physics2D.Raycast(start, direction, distance, collisionMask);
                if (!hit) continue;

                body.position -= direction * (distance - hit.distance);
                body.velocity += direction * Mathf.Min(0.0f, Vector2.Dot(-body.velocity, direction));
            }
        }

        private void CheckForGround()
        {
            if (body.velocity.y > float.Epsilon)
            {
                isOnGround = false;
                return;
            }

            var iterations = 6;
            for (var i = 0; i < iterations; i++)
            {
                var position = body.position + Vector2.up * body.velocity.y * Time.deltaTime;
                var offset = Vector2.right * ((i / (iterations - 1.0f) - 0.5f) * collisionSize.x + collisionOffset.x) * 0.9f;

                var hit = Physics2D.Raycast(position + offset + Vector2.up * stepHeight, Vector2.down, stepHeight, collisionMask);
                if (!hit) continue;

                groundHit = hit;
                isOnGround = true;
                body.position += Vector2.up * (groundHit.point.y - body.position.y);
                body.velocity += Vector2.up * Mathf.Max(0.0f, -body.velocity.y);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawWireCube(collisionOffset, collisionSize);
        }
    }
}