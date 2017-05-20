using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inherent this for the singleton pattern. 
/// Note that this is not a true singleton, as more than one instance can be achieved through loading multiple scenes.
/// </summary>
/// <typeparam name="T">The Component that should be a singleton</typeparam>
public class Singleton<T> : MonoBehaviour where T : Component {

    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.hideFlags = HideFlags.HideAndDontSave;
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }
}
