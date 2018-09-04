using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace ElmoGameNetwork
{
    public class MyTime : MonoBehaviour
    {


        struct MyAction
        {
            public string guid;
            public System.Action action;
            public MyAction(string id, System.Action action)
            {
                this.guid = id;
                this.action = action;
            }
        }
        private static SortedList<float, MyAction> timeslist;
        private static SortedList<float, MyAction> timeslist2;
        private static List<string> cancelledActions;
        private static Stopwatch stopwatch;
        private static float DeltaTime;
        private float LastFrameTime;
        private float Elapsed;

        void Awake()
        {
            stopwatch = new Stopwatch();
            timeslist = new SortedList<float, MyAction>();
            timeslist2 = new SortedList<float, MyAction>();
            cancelledActions = new List<string>();
            _Start();
            DeltaTime = 0;
            LastFrameTime = stopwatch.ElapsedMilliseconds;
        }
        void Update()
        {
            Elapsed = stopwatch.ElapsedMilliseconds / 1000f;
            DeltaTime = (Elapsed - LastFrameTime);
            LastFrameTime = Elapsed;

            if(timeslist.Count > 0)
            {

                if(Time.time > timeslist.Keys[0])
                {
                    MyAction myAction = timeslist[timeslist.Keys[0]];
                    string id = myAction.guid;

                    if(!cancelledActions.Contains(id))
                    {
                        myAction.action();

                    }
                    else
                    {
                        cancelledActions.Remove(id);
                    }
                    timeslist.RemoveAt(0);
                }

            }
            if(timeslist2.Count > 0)
            {
                if(Elapsed > timeslist2.Keys[0])
                {

                    MyAction myAction = timeslist2[timeslist2.Keys[0]];
                    string id = myAction.guid;

                    if(!cancelledActions.Contains(id))
                    {
                        myAction.action();

                    }
                    else
                    {
                        cancelledActions.Remove(id);
                    }
                    timeslist2.RemoveAt(0);

                }
            }

        }
        public static string IInvoke(System.Action MyMethode, float time)
        {
            System.Guid guid = System.Guid.NewGuid();
            float extra = 0;
            while(timeslist.ContainsKey(Time.time + time + extra))
            {
                extra += 0.01f;
            }
            timeslist.Add(Time.time + time + extra, new MyAction(guid.ToString(), MyMethode));

            return guid.ToString();
        }
        public static string IInvoke_Not_Global(System.Action MyMethode, float time)
        {
            System.Guid guid = System.Guid.NewGuid();
            float extra = 0;
            while(timeslist2.ContainsKey((stopwatch.ElapsedMilliseconds / 1000f) + time))
            {
                extra += 0.01f;
            }
            timeslist2.Add((stopwatch.ElapsedMilliseconds / 1000f) + time + extra, new MyAction(guid.ToString(), MyMethode));

            return guid.ToString();
        }
        public static void IInvokeCancel(string _id)
        {
            cancelledActions.Add(_id);
        }
        public static void _Start()
        {
            stopwatch.Start();
        }
        public static void _Stop()
        {
            stopwatch.Stop();

        }
        public static void _Reset()
        {
            stopwatch.Reset();
        }
        public static float _Elapsed()
        {
            return stopwatch.ElapsedMilliseconds / 1000f;
        }

    }
}
