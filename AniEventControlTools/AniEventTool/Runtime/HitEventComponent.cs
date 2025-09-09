using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AniEventTool
{
    public class HitEventComponent : MonoBehaviour
    {
        private HashSet<GameObject> enteredObjects;

        private void Awake()
        {
            enteredObjects = new HashSet<GameObject>();
        }

        public void AddObject(GameObject obj)
        {
            if (enteredObjects.Add(obj))
            {
                // Optional: Perform actions when a new object is added
                Debug.Log("New object added: " + obj.name);
            }
        }

        public void RemoveObject(GameObject obj)
        {
            if (enteredObjects.Remove(obj))
            {
                // Optional: Perform actions when an object is removed
                Debug.Log("Object removed: " + obj.name);
            }
        }

        public List<GameObject> GetEnteredObjects()
        {
            return new List<GameObject>(enteredObjects);
        }
    }
}