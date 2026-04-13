using UnityEngine;

namespace Cumic
{
    public static class Util
    {
        #region Component

        /// <summary>
        /// 컴포넌트가 있으면 가져오고, 없으면 추가합니다.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 컴포넌트가 있으면 가져오고, 없으면 추가합니다.
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }

        #endregion

        #region Transform

        /// <summary>
        /// Transform의 모든 자식 오브젝트를 삭제합니다.
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Transform의 모든 자식 오브젝트를 즉시 삭제합니다.
        /// </summary>
        public static void DestroyChildrenImmediate(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        #endregion

        #region Vector With Pattern

        /// <summary>
        /// X 값만 변경한 새 Vector2를 반환합니다.
        /// </summary>
        public static Vector2 WithX(this Vector2 vector, float x)
        {
            return new Vector2(x, vector.y);
        }

        /// <summary>
        /// Y 값만 변경한 새 Vector2를 반환합니다.
        /// </summary>
        public static Vector2 WithY(this Vector2 vector, float y)
        {
            return new Vector2(vector.x, y);
        }

        /// <summary>
        /// X 값만 변경한 새 Vector3를 반환합니다.
        /// </summary>
        public static Vector3 WithX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }

        /// <summary>
        /// Y 값만 변경한 새 Vector3를 반환합니다.
        /// </summary>
        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }

        /// <summary>
        /// Z 값만 변경한 새 Vector3를 반환합니다.
        /// </summary>
        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }

        #endregion

        #region LayerMask

        /// <summary>
        /// LayerMask에 특정 레이어가 포함되어 있는지 확인합니다.
        /// </summary>
        public static bool Contains(this LayerMask layerMask, int layer)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        /// <summary>
        /// LayerMask에 특정 GameObject의 레이어가 포함되어 있는지 확인합니다.
        /// </summary>
        public static bool Contains(this LayerMask layerMask, GameObject gameObject)
        {
            return layerMask.Contains(gameObject.layer);
        }

        #endregion
    }
}
