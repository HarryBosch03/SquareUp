using System;
using SquareUp.Runtime.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SquareUp.Runtime.Player
{
    [SelectionBase, DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        public InputActionAsset inputAsset;

        [Space]
        public float moveSpeed = 10.0f;
        public float accelerationTime = 0.1f;
        [Range(0.0f, 1.0f)]
        public float airMovementPenalty = 0.9f;
        public float stepHeight = 0.51f;

        [Space]
        public float jumpHeight = 2.0f;
        public float upGravity = 2.0f;
        public float downGravity = 3.0f;

        [Space]
        public Vector2 collisionOffset = new(0.0f, 0.5f);
        public Vector2 collisionSize = new(0.5f, 1.0f);
        public LayerMask collisionMask = ~0;

        [Space]
        public Projectile projectilePrefab;
        public float fireDelay = 0.1f;
        public int projectilesPerShot = 1;
        public float spray = 2.0f;

        [Space]
        [Range(0.0f, 1.0f)]
        public float animationSmoothing;

        private Rigidbody2D body;
        private Animator animator;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction shootAction;

        private bool jumpFlag;
        private bool isOnGround;
        private RaycastHit2D groundHit;
        private bool isOnWall;
        private RaycastHit2D wallHit;

        private float rotation;
        private Transform visuals;
        private Transform head;
        private Transform leftArm;
        private Transform muzzle;

        private Vector2 lookPosition;
        private Camera mainCam;

        private float lastFireTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            mainCam = Camera.main;

            visuals = transform.Find("Visuals");
            head = visuals.Find("Head");
            leftArm = visuals.Find("Arm.L");
            muzzle = visuals.Find("Arm.L/Gun/Muzzle");

            inputAsset.Enable();
            moveAction = inputAsset.FindAction("Move");
            jumpAction = inputAsset.FindAction("Jump");
            shootAction = inputAsset.FindAction("Shoot");

            foreach (var t in GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = 7;
            }
        }

        private void FixedUpdate()
        {
            Move();
            Jump();
            Shoot();
            Collide();
            SetGravity();

            var dir = lookPosition - body.position;
            rotation = Mathf.Lerp(dir.x > 0.0f ? 1 : -1, rotation, animationSmoothing);

            jumpFlag = false;
        }

        private void Shoot()
        {
            if (!shootAction.IsPressed()) return;
            if (Time.time - lastFireTime < fireDelay) return;

            for (var i = 0; i < projectilesPerShot; i++)
            {
                var rotation = muzzle.eulerAngles.z + Random.Range(-spray, spray);
                Instantiate(projectilePrefab, muzzle.position, Quaternion.Euler(0.0f, 0.0f, rotation));
            }

            lastFireTime = Time.time;
        }

        private void SetGravity() { body.gravityScale = body.velocity.y > 0.0f ? upGravity : downGravity; }

        private void Update()
        {
            if (jumpAction.WasPerformedThisFrame()) jumpFlag = true;
            lookPosition = GetMousePosition();
        }

        private void LateUpdate() { Animate(); }

        private void Animate()
        {
            visuals.rotation = Quaternion.Euler(0.0f, rotation * 90.0f - 90.0f, 0.0f);

            var headDir = lookPosition - (Vector2)head.position;
            head.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(headDir.y, Mathf.Abs(headDir.x)) * Mathf.Rad2Deg * 0.6f);

            var armDir = lookPosition - (Vector2)leftArm.position;
            leftArm.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(armDir.y, armDir.x) * Mathf.Rad2Deg + 90.0f) * visuals.rotation;

            animator.SetBool("falling", !isOnGround);
            animator.SetFloat("movement", isOnGround ? body.velocity.x / moveSpeed : 0.0f);
        }

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
            if (!isOnGround) return;

            var gravity = -Physics2D.gravity.y * upGravity;
            var force = Mathf.Sqrt(2.0f * jumpHeight * gravity);
            body.velocity += Vector2.up * force;
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