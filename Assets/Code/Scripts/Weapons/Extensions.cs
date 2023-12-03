using UnityEngine;

namespace SquareUp.Runtime.Weapons
{
    public static class Extensions
    {
        public static Vector2 FromDeg(this float deg)
        {
            var rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public static float ToDeg(this Vector2 v)
        {
            v.Normalize();
            return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        }

        public static Quaternion AsRotation(this float deg) => Quaternion.Euler(0.0f, 0.0f, deg);

        public static Quaternion AsRotation(this Vector2 v) => v.ToDeg().AsRotation();

        public static T Find<T>(this Transform transform, string path)
        {
            var find = transform.Find(path);
            return find ? find.GetComponent<T>() : default;
        }
    }
}