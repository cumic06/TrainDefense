using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager _instance;
    public static ResourceManager Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }

    private readonly Dictionary<string, Stack<GameObject>> _pools = new();

    public GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogError("ResourceManager: Cannot spawn null prefab");
            return null;
        }

        var key = GetPoolKey(prefab);

        if (!_pools.ContainsKey(key))
        {
            _pools.Add(key, new Stack<GameObject>());
        }

        if (_pools[key].Count > 0)
        {
            GameObject popObject = _pools[key].Pop();

            popObject.SetActive(true);
            popObject.transform.SetPositionAndRotation(position, rotation);

            if (parent != null)
            {
                popObject.transform.SetParent(parent);
            }
            else
            {
                popObject.transform.SetParent(transform);
            }

            return popObject;
        }
        else
        {
            return CreateNewObject(prefab, position, rotation, parent);
        }
    }

    public T Spawn<T>(T prefab, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null) where T : Component
    {
        GameObject spawnedObject = Spawn(prefab.gameObject, position, rotation, parent);
        return spawnedObject.GetComponent<T>();
    }

    public GameObject SpawnPath(string path, Vector3 position = default, Quaternion rotation = default,
        Transform parent = null)
    {
        var spawnObject = Resources.Load<GameObject>(path);

        if (spawnObject == null)
        {
            Debug.LogWarning("path can't be found");
            return null;
        }

        return Spawn(spawnObject, position, rotation, parent);
    }

    public void Destroy(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("ResourceManager: Cannot destroy null object");
            return;
        }

        string key = GetPoolKey(obj);

        if (!_pools.ContainsKey(key))
        {
            _pools.Add(key, new Stack<GameObject>());
        }

        _pools[key].Push(obj);
        obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.transform.SetParent(transform);
        obj.SetActive(false);
    }

    private string GetPoolKey(GameObject obj)
    {
        string key = obj.name;
        key = key.Replace("(Clone)", "");
        return key;
    }

    private GameObject CreateNewObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject spawnObject;
        if (parent != null)
        {
            spawnObject = Instantiate(prefab, position, rotation, parent);
        }
        else
        {
            spawnObject = Instantiate(prefab, position, rotation, transform);
        }
        return spawnObject;
    }
}