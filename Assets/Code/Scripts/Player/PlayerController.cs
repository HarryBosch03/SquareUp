using UnityEngine;
using UnityEngine.InputSystem;

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
        [Range(0.0f, 1.0f)]
        public float animationSmoothing;

        private Rigidbody2D body;

        private InputAction moveAction;
        private InputAction jumpAction;

        private bool jumpFlag;
        private bool isOnGround;
        private RaycastHit2D groundHit;
        
        private float rotation;
        private Transform visuals;
        private Transform head;

        private Vector2 lookPosition;
        private Camera mainCam;
        
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            mainCam = Camera.main;
            
            visuals = transform.Find("Visuals");
            head = visuals.Find("Head");

            inputAsset.Enable();
            moveAction = inputAsset.FindAction("Move");
            jumpAction = inputAsset.FindAction("Jump");

            foreach (var t in GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = 7;
            }
        }

        private void FixedUpdate()
        {
            Move();
            Jump();
            Collide();
            SetGravity();

            var dir = lookPosition - body.position;
            rotation = Mathf.Lerp(dir.x > 0.0f ? 1 : -1, rotation, animationSmoothing);

            jumpFlag = false;
        }

        private void SetGravity()
        {
            body.gravityScale = body.velocity.y > 0.0f ? upGravity : downGravity;
        }

        private void Update()
        {
            if (jumpAction.WasPerformedThisFrame()) jumpFlag = true;
            
            Animate();

            lookPosition = GetMousePosition();
        }

        private void Animate()
        {
            visuals.rotation = Quaternion.Euler(0.0f, rotation * 90.0f - 90.0f, 0.0f);

            var headDir = lookPosition - (Vector2)head.position;
            head.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(headDir.y, headDir.x) * Mathf.Rad2Deg + (rotation * 90.0f - 90.0f)) * visuals.rotation;
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
            
            Collide(Vector2.right, new Vector2(0.0f, offset.y), Vector2.up * size.y, size.x * 0.5f);
            Collide(Vector2.left,  new Vector2(0.0f, offset.y), Vector2.up * size.y, size.x * 0.5f);
        }
        
        private void Collide(Vector2 direction, Vector2 offset, Vector2 spread, float distance, int iterations = 16)
        {
            for (var i = 0; i < iterations; i++)
            {
                var position = body.position;
                var localOffset = spread * ((iterations > 1 ? i / (iterations - 1.0f) : 0.5f) - 0.5f);

                var start = position + offset + localOffset;
                var hit = Physics2D.Raycast(start, direction, distance, collisionMask);
                Debug.DrawRay(start, direction * distance, groundHit ? Color.green : Color.red);
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

            for (var i = 0; i < 16; i++)
            {
                var position = body.position + (Vector2.up * body.velocity.y * Time.deltaTime);
                var offset = Vector2.right * ((i / 15.0f - 0.5f) * collisionSize.x + collisionOffset.x) * 0.9f;

                groundHit = Physics2D.Raycast(position + offset + Vector2.up * stepHeight, Vector2.down, stepHeight, collisionMask);
                Debug.DrawRay(position + offset + Vector2.up * stepHeight, Vector2.down * stepHeight, groundHit ? Color.green : Color.red);
                if (!groundHit) continue;

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