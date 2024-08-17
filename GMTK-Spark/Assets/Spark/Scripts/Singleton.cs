using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        Instance = this as T;
    }

    private void OnApplicationQuit()
    {
        Instance = null;
        Destroy(gameObject);
    }
}
