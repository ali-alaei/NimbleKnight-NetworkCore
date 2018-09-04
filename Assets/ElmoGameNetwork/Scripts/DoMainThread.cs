using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace ElmoGameNetwork
{

    public class DoMainThread : MonoBehaviour
    {

        private readonly static Queue<Action> actionQueue = new Queue<Action>();
        private readonly static List<Action> QueueForExit = new List<Action>();

        void Awake()
        {
            StartCoroutine(Execute());
        }
        IEnumerator Execute()
        {
            while (true)
            {
                // dispatch stuff on main thread
                while (actionQueue.Count > 0)
                {
                    actionQueue.Dequeue().Invoke();
                }
                yield return null;
            }
        }
        public static void ExecuteOnMainThread(Action action)
        {
            actionQueue.Enqueue(action);
        }
        public static void ExecuteOnExit(Action action)
        {
            QueueForExit.Add(action);
        }
        private void OnDestroy()
        {
            foreach (var item in QueueForExit)
            {
                item();
            }
        }
    }
}
