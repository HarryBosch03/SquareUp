using SquareUp.Runtime.Weapons;
using UnityEngine;

namespace SquareUp.Runtime.Player
{
    public class Arm
    {
        private const int Resolution = 16;
        private const float Reach = 1.0f;
        private const float TotalLength = 1.0f;

        public Transform hand;
        public LineRenderer arm;
        
        public Arm(Transform root, char l)
        {
            arm = root.Find<LineRenderer>($"Arm.{l}");
            hand = root.Find($"Hand.{l}");

            arm.useWorldSpace = true;
            arm.positionCount = Resolution;
        }

        public void Update(Vector2 position)
        {
            var vector = Vector2.ClampMagnitude(position - (Vector2)arm.transform.position, Reach);
            position = vector + (Vector2)arm.transform.position;
            
            hand.position = position;

            var l0 = vector.magnitude;
            var l2 = TotalLength;
            var l1 = l2 > l0 ? Mathf.Sqrt(l2 * l2 - l0 * l0) : 0.0f;
            
            var a = (Vector2)arm.transform.position;
            var c = (Vector2)hand.position;
            var b = (a + c) * 0.5f + Vector2.down * l1;
            
            for (var i = 0; i < arm.positionCount; i++)
            {
                var p = i / (arm.positionCount - 1.0f);
                var ab = Vector2.Lerp(a, b, p);
                var bc = Vector2.Lerp(b, c, p);
                var abc = Vector2.Lerp(ab, bc, p);
                
                arm.SetPosition(i, abc);
            }
        }
    }
}