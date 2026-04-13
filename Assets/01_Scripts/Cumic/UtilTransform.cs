using UnityEngine;

namespace Cumic
{
    public static class UtilTransform
    {
        #region Reset

        /// <summary>
        /// Transform의 position, rotation, localScale을 초기값으로 리셋합니다.
        /// </summary>
        public static void ResetTransformation(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Transform의 localPosition, localRotation, localScale을 초기값으로 리셋합니다.
        /// </summary>
        public static void ResetLocalTransformation(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        #endregion

        #region LookAt 2D

        /// <summary>
        /// 2D에서 타겟 위치를 바라보도록 회전합니다. (Z축 회전)
        /// </summary>
        public static void LookAt2D(this Transform transform, Vector2 target)
        {
            Vector2 direction = target - (Vector2)transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// 2D에서 타겟 Transform을 바라보도록 회전합니다. (Z축 회전)
        /// </summary>
        public static void LookAt2D(this Transform transform, Transform target)
        {
            transform.LookAt2D(target.position);
        }

        /// <summary>
        /// 2D에서 타겟 방향의 각도를 반환합니다. (degrees)
        /// </summary>
        public static float GetAngle2D(this Transform transform, Vector2 target)
        {
            Vector2 direction = target - (Vector2)transform.position;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 2D 방향 벡터의 각도를 반환합니다. (degrees)
        /// </summary>
        public static float ToAngle(this Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        #endregion
    }
}
