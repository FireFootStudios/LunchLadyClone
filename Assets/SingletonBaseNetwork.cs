using Unity.Netcode;
using UnityEngine;

//This script might or might not be 'inspired' by an AI :).

public class SingletonBaseNetwork<T> : NetworkBehaviour where T : NetworkBehaviour
{
    protected static T _instance;
    private static bool _isQuitting = false;
    protected bool _isDestroying = false;

    public bool IsDestroying { get { return _isDestroying; } }

    public static T Instance
    {
        get
        {
            // If the instance is already set, return it
            if (_instance != null)
                return _instance;

            //prevent a new instance/gameobject being created when app is quitting
            if (_isQuitting) return null;

            // Try to find an existing instance in the scene
            _instance = FindObjectOfType<T>();

            // If no existing instance is found, create a new one
            if (_instance == null)
            {
                GameObject singletonObject = new GameObject(typeof(T).Name);
                _instance = singletonObject.AddComponent<T>();
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // Ensure that there is only one instance of the singleton
        if (_instance != null && _instance != this)
        {
            _isDestroying = true;

            //Destroy(gameObject);

            //Temporary fix to this object (and mostly scripts on or under it) existing for 1 frame and doing stuff they shouldnt
            DestroyImmediate(gameObject);
            
            return;
        }
        else _instance = this as T;

        // Make the instance persist between scenes
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}