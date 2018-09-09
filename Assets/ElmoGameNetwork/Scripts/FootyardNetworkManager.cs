using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//using ElmoGameJson;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Timers;
namespace ElmoGameNetwork
{


    public class NetworkManager : MonoBehaviour
    {
        #region Singleton Pattern

        static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<NetworkManager>();
                return _instance;
            }
        }

        #endregion


        #region ATTRIBUTES
        private string playerId = "1";
        NetworkCore client;
        public DEBUG_LEVEL debugLevel;
        private float lastPongTime;
        private Coroutine recennectCoRoutin;
        private Coroutine pingRoutin;
        private Coroutine checkLastPingRoutin;
        long seqNumber = 0;
        private Dictionary<long, JSONObject> keepList;
        public string symmetricKey;
        string sessionKey;
        long lastSequenceNumber;
        System.Diagnostics.Stopwatch timer;
        bool isConnected = false;
        private const int Sending_Ping_Intervel = 6;
        private const int Sending_Ping_TimeOut = 20;
        private const int Check_Ping_Intervel = 2;
        private const int Stop_Sending_Ping_TimeOut = 180;

        #endregion

        #region  EVENTS
        public Action<ServerResponse> OnConnectAction;

        public delegate void SubscribeToFunction(Functions function, ServerResponse response, JSONObject content);
        public delegate void SubscribeToMatchFunction(string c_functionName, ServerResponse response, JSONObject content);
        public delegate void SubscribeTo_AI_MatchFunction(string c_functionName, ServerResponse response, JSONObject content);
        public delegate void OnAlreadyInGameEvent(JSONObject content);
        public delegate void OnDisconnect();
        public delegate void OnJoinRecieved();
        public SubscribeToFunction subscribeToFunction;
        public SubscribeToMatchFunction subscribeToMatchFunction;
        public SubscribeTo_AI_MatchFunction subscribeTo_AI_MatchFunction;
        public OnAlreadyInGameEvent OnAlreadyInGameEventFunction;
        public OnDisconnect onDisconnectFunction;
        public OnJoinRecieved onJoinRecievedFunction;

        #endregion
        private static Timer pingTimer;
        private static Timer applicationFocusTimer = new Timer();
        void Awake()
        {
            client = NetworkCore.Instance;
            client.Initialize();
            sessionKey = symmetricKey;

            keepList = new Dictionary<long, JSONObject>();

            client.OnMessageReceived += OnMessageReceived;
            client.OnDesconnected += OnDesconnected;
            client.OnSubscribe += OnSubscribe;
            client.OnCouldNotConnect += onCouldentConnect;    
            pingTimer = new System.Timers.Timer();
            pingTimer.Elapsed += OnPingTimerEvent;
            pingTimer.Interval = Sending_Ping_Intervel * 1000;
        }

        private void OnApplicationFocus(bool focus)
        {
            try
            {
                if (focus == false)
                {
                    print("focus falsed");
                    applicationFocusTimer = new Timer();
                    applicationFocusTimer.Interval = Stop_Sending_Ping_TimeOut*1000;
                    applicationFocusTimer.Start();
                    applicationFocusTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
                    {
                        pingTimer.Stop();
                    };
                }
                else
                {
                    print("focus true");

                    applicationFocusTimer.Stop();
                    if (client!=null && isConnected)
                        StartSendingPing();
                }
            }
            catch(System.Exception ex)
            {
                Debug.LogError("Expection in OnApplicationFocus" + ex.ToString());
            }
        }

        private void onCouldentConnect()
        {
            NotificationHandeler.present(NotificationHandeler.Keys.CouldNotConnectToSocket, 
                NotificationHandeler.NotifType.Problem, 5);
        }

        private void OnDestroy()
        {
            pingTimer.Stop();
            pingTimer.Elapsed -= OnPingTimerEvent;

        }
        
        private void OnApplicationQuit()
        {
            pingTimer.Stop();
            pingTimer.Elapsed -= OnPingTimerEvent;

        }



        #region ENUMS

        public enum DEBUG_LEVEL
        {
            ON,
            OFF,
        }

        public enum RequestType
        {
            service,
            exchange,
        }
        public enum Functions
        {
            echo,
            chat,
            join_room,
            heartbeat,
            match_request,
            cancel_match_request,
            start_match,
            match_load_complete,
            friendly_match_request,
            accept_friendly_match_request,
            reject_friendly_match_request,
            match,
            rematch_request,
            reject_rematch_request,
            accept_rematch_request,
            ai_match,
            ai_match_load_complete,
            cancel_rematch_request,
            tournament_request,
            tournament_new_player,
            cancel_friendly_match_request,
            match_load_complete_yet,
        }
        public enum ServerResponse
        {
            OK = 200,
            ACCEPTED = 202,
            PLAYER_BUSY = 272,
            ALREADY_INGAME = 282,
            ALREADY_JOIN = 292,
            BAD_REQUEST = 400,
            UNATHORIZED = 401,
            NOT_FOUND = 404,
            NOT_ACCEPTABLE = 406,
            NOT_IMPLEMENTED = 501,


        }

        #endregion    

        #region Messaging
        // Update is called once per frame
        public void SendServiceMessage(JSONObject content, Functions func, bool justSymmetricKey = false)
        {
            timer = new System.Diagnostics.Stopwatch();
            seqNumber++;
            JSONObject data = new JSONObject();
            data.AddField("content", content);
            data.AddField("reply_to", client.RoutingKey);
            data.AddField("sender", playerId);
            data.AddField("seq_id", seqNumber);
            data.AddField("type", RequestType.service.ToString());
            data.AddField("function", func.ToString());
            JSONObject final = CalculateFinalSecureJson(data, justSymmetricKey);
            if (func != Functions.heartbeat)
                WriteToConsole("SendMessageToServer: " + data.ToString());
            timer.Start();
            client.PublishMessage(final.ToString());
        }
        public void SendExchangeMessage(JSONObject content, string opponent_id, Functions func, bool useAke = false)
        {
            seqNumber++;

            JSONObject data = new JSONObject();
            //  print(content);
            data.AddField("content", content);
            data.AddField("reply_to", client.RoutingKey);
            data.AddField("sender", playerId);
            data.AddField("opponent_id", opponent_id);
            data.AddField("seq_id", seqNumber);
            data.AddField("type", RequestType.exchange.ToString());
            data.AddField("function", func.ToString());
            WriteToConsole("SendMessageToUser: " + data.ToString());

            JSONObject final = CalculateFinalSecureJson(data);

            if (useAke)
                keepList.Add(seqNumber, data);

            client.PublishMessage(final.ToString());
        }
        public void SendExchangeMessage(JSONObject content, string opponent_id, Functions func, string sender, bool useAke = false)
        {
            seqNumber++;

            JSONObject data = new JSONObject();
            //  print(content);
            data.AddField("content", content);
            data.AddField("reply_to", client.RoutingKey);
            data.AddField("sender", sender);
            data.AddField("opponent_id", opponent_id);
            data.AddField("seq_id", seqNumber);
            data.AddField("type", RequestType.exchange.ToString());
            data.AddField("function", func.ToString());

            WriteToConsole("SendMessageToUser: " + data.ToString());

            JSONObject final = CalculateFinalSecureJson(data);

            if (useAke)
                keepList.Add(seqNumber, data);



            client.PublishMessage(final.ToString());



        }

        public void SendMatchMessage(JSONObject content, string opponent_id, string custumFunctionName, string gameId, bool useAke = true)
        {
            seqNumber++;

            JSONObject data = new JSONObject();
            data.AddField("content", content);
            data.AddField("reply_to", client.RoutingKey);
            data.AddField("sender", playerId);
            data.AddField("opponent_id", opponent_id);
            data.AddField("seq_id", seqNumber);
            data.AddField("type", RequestType.exchange.ToString());
            data.AddField("function", Functions.match.ToString());
            data.AddField("c_function", custumFunctionName);
            data.AddField("game_id", gameId);

            WriteToConsole("SendMatchMessage: " + data.ToString());

            JSONObject final = CalculateFinalSecureJson(data);

            if (useAke)
                keepList.Add(seqNumber, data);



            client.PublishMessage(final.ToString());



        }

        public void SendMatchMessage(JSONObject content, string opponent_id, string custumFunctionName, string gameId, string sender, bool useAke = true)
        {
            seqNumber++;

            JSONObject data = new JSONObject();
            data.AddField("content", content);
            data.AddField("reply_to", client.RoutingKey);
            data.AddField("sender", sender);
            data.AddField("opponent_id", opponent_id);
            data.AddField("seq_id", seqNumber);
            data.AddField("type", RequestType.exchange.ToString());
            data.AddField("function", Functions.match.ToString());
            data.AddField("c_function", custumFunctionName);
            data.AddField("game_id", gameId);

            WriteToConsole("SendMatchMessage: " + data.ToString());

            JSONObject final = CalculateFinalSecureJson(data);

            if (useAke)
                keepList.Add(seqNumber, data);



            client.PublishMessage(final.ToString());



        }

        public void Send_AI_MatchMessage(JSONObject content, string opponent_id, string custumFunctionName, string gameId, string sender, bool useAke = true)
        {
            seqNumber++;

            JSONObject data = new JSONObject();
            data.AddField("content", content);
            data.AddField("reply_to", client.RoutingKey);

            data.AddField("sender", sender);
            data.AddField("opponent_id", opponent_id);
            data.AddField("seq_id", seqNumber);
            data.AddField("type", RequestType.exchange.ToString());
            data.AddField("function", Functions.ai_match.ToString());
            data.AddField("c_function", custumFunctionName);
            data.AddField("game_id", gameId);

            WriteToConsole("SendMatchMessage: " + data.ToString());

            JSONObject final = CalculateFinalSecureJson(data);

            if (useAke)
                keepList.Add(seqNumber, data);



            client.PublishMessage(final.ToString());


        }
        public void Send_AI_MatchMessage(JSONObject content, string opponent_id, string custumFunctionName, string gameId, bool useAke = true)
        {
            seqNumber++;

            JSONObject data = new JSONObject();
            data.AddField("content", content);
            data.AddField("reply_to", client.RoutingKey);
            data.AddField("sender", playerId);
            data.AddField("opponent_id", opponent_id);
            data.AddField("seq_id", seqNumber);
            data.AddField("type", RequestType.exchange.ToString());
            data.AddField("function", Functions.ai_match.ToString());
            data.AddField("c_function", custumFunctionName);
            data.AddField("game_id", gameId);

            WriteToConsole("SendMatchMessage: " + data.ToString());

            JSONObject final = CalculateFinalSecureJson(data);

            if (useAke)
                keepList.Add(seqNumber, data);



            client.PublishMessage(final.ToString());



        }

        public void SendEcho(string str)
        {
            JSONObject temp = new JSONObject();
            temp.AddField("Message", str);
            SendServiceMessage(temp, Functions.echo);
        }

        void OnMessageReceived(string text)
        {
            // print("DeliveryTag: " + received.Message.DeliveryTag);
            JSONObject json = new JSONObject(text);
            JSONObject content = json.GetField("content");
            RequestType type = (RequestType)Enum.Parse(typeof(RequestType), json.GetField("type").str);
            Functions function = (Functions)Enum.Parse(typeof(Functions), json.GetField("function").str);
            long seqNumber = -1;
            if (json.HasField("seq_id"))
                seqNumber = (long)json.GetField("seq_id").n;
            ServerResponse response = (ServerResponse)int.Parse(json.GetField("response").str);
            string c_function = "";
            if (function != Functions.heartbeat)
                WriteToConsole("OnExhangeMessageRecived" + json.ToString());

            switch (type)
            {
                case RequestType.service:
                    switch (function)
                    {
                        case Functions.echo:
                            SendEcho_Received(response, content);
                            break;
                        case Functions.chat:
                            break;
                        case Functions.join_room:
                            Debug.Log("join Resceive");
                            List<JSONObject> nonceList = json.GetField("nonce").list;
                            int[] nonce = new int[2];
                            nonce[0] = (int)nonceList[0].n;
                            nonce[1] = (int)nonceList[1].n;

                            JoinRoom_Received(response, nonce, content);
                            break;
                        case Functions.heartbeat:
                            SendPing_Receive(response);
                            break;

                        default:
                            break;
                    }

                    break;
                case RequestType.exchange:

                    RemoveItemFromSafeKeep(seqNumber);
                    switch (function)
                    {

                        case Functions.match:
                            c_function = json.GetField("c_function").str;
                            break;
                        case Functions.ai_match:
                            c_function = json.GetField("c_function").str;
                            break;
                        default:
                            break;
                    }
                    UpdateSequenceNumber(seqNumber);

                    break;
                default:
                    break;
            }

            InvokeSubscribedActions(function, json, response, c_function);
        }
        void SendEcho_Received(ServerResponse response, JSONObject content)
        {
            if (response == ServerResponse.OK)
            {
                WriteToConsole(content.ToString());
            }
        }

        #endregion


        #region INTERNAL FUNCTIONS
        string GenerateNonce(int nonce1, int nonce2)
        {

            string temp = Encryption.GenerateNonce(nonce1, nonce2);
            return temp;
        }
        void UpdateSequenceNumber(long seqNumber)
        {
            lastSequenceNumber = seqNumber;
        }
        void InvokeSubscribedActions(Functions function, JSONObject content, ServerResponse response, string c_function)
        {
            subscribeToFunction?.Invoke(function, response, content);
            if (function == Functions.match && subscribeToMatchFunction != null)
                subscribeToMatchFunction(c_function, response, content);
            if (function == Functions.ai_match && subscribeTo_AI_MatchFunction != null)
                subscribeTo_AI_MatchFunction(c_function, response, content);
        }
        JSONObject CalculateFinalSecureJson(JSONObject payload, bool justSymetricKey = false)
        {
            JSONObject data = new JSONObject();
            //JSONObject raw = new JSONObject();
            string base64Payload = Encryption.StringToBase64(payload.ToString());
            data.AddField("payload", base64Payload);
            //raw.AddField("payload", base64Payload);
            // raw.AddField("hmac", base64Payload);
            // print(raw);
            if (!justSymetricKey)
                data.AddField("hmac", Encryption.GetHashString(base64Payload, sessionKey));
            else
                data.AddField("hmac", Encryption.GetHashString(base64Payload, symmetricKey));
            return data;
        }

        #endregion

        #region PING
        void SendPing()
        {
           // Debug.Log("Send ping");
            SendServiceMessage(null, Functions.heartbeat);
        }

        void SendPing_Receive(ServerResponse response)
        {
            if (response == ServerResponse.OK)
            {
                lastPongTime = Time.realtimeSinceStartup;
            }
            else if (response == ServerResponse.NOT_FOUND)
            {
                JoinRoom();
            }
        }
        
        void StartSendingPing()
        {
            pingTimer.Stop();
            pingTimer.Start();
        }
        
        IEnumerator CheckLastPong()
        {
            while (true)
            {
                if (isConnected)
                {
                    //print(Time.realtimeSinceStartup - lastPongTime);
                    if (Time.realtimeSinceStartup - lastPongTime > Sending_Ping_TimeOut)
                    {
                        OnPingTimeOut();
                    }
                }
                yield return new WaitForSeconds(Check_Ping_Intervel);
            }
        }

        #endregion
        #region SAFEKEEP
        public void ClearSafeKeep()
        {
            keepList.Clear();
        }
        void RemoveItemFromSafeKeep(long id)
        {
            if (keepList.ContainsKey(id))
                keepList.Remove(id);
        }
        public void SendSafeKeep()
        {
            foreach (KeyValuePair<long, JSONObject> req in keepList)
            {
                print("SendSafeKeep: " + req.Value);
                SendLostMessages(req.Value);
            }
        }
        void SendLostMessages(JSONObject data)
        {
            // data.AddField("reply_to", client.RoutingKey);


            data.RemoveField("reply_to");
            data.AddField("reply_to", client.RoutingKey);
            JSONObject final = CalculateFinalSecureJson(data);

            client.PublishMessage(final.ToString());
        }
        #endregion

        #region ROOM
        void JoinRoom()
        {
            print("Join Room Sent");
            SendServiceMessage(null, Functions.join_room, true);
        }
        void JoinRoom_Received(ServerResponse response, int[] nonces, JSONObject content)
        {
            OnConnectAction?.Invoke(response);
            string nonceKey = GenerateNonce(nonces[0], nonces[1]);
            sessionKey = symmetricKey + nonceKey;
            isConnected = true;
            lastPongTime = Time.realtimeSinceStartup;
            if (pingRoutin != null)
                StopCoroutine(pingRoutin);
            if (checkLastPingRoutin != null)
                StopCoroutine(checkLastPingRoutin);


            StartSendingPing();
            checkLastPingRoutin = StartCoroutine(CheckLastPong());

            if (response == ServerResponse.ACCEPTED)
            {
                onJoinRecievedFunction?.Invoke();
            }
            else if (response == ServerResponse.ALREADY_JOIN)
            {

                onJoinRecievedFunction?.Invoke();
            }
            else if (response == ServerResponse.ALREADY_INGAME)
            {

                Debug.LogError("ALREADY_INGAME");
                OnAlreadyInGameEventFunction?.Invoke(content);
            }
        }

        private void OnPingTimerEvent(object sender, ElapsedEventArgs e)
        {
            SendPing();

        }
        #endregion

        #region CONNECTION
        public void Connect(string player_Id, bool forceNewConnect = false)
        {
            if (!isConnected || forceNewConnect)
            {

                playerId = player_Id;

                client.Connect();
            }
            else
            {
                print("already Connected");
            }

        }

        void OnSubscribe()
        {
            JoinRoom();
        }

        void OnDesconnected()
        {
            if (isConnected == true)
            {
                onDisconnectFunction?.Invoke();
            }
            isConnected = false;
        }
        void OnPingTimeOut()
        {
            if (isConnected == true)
            {
                Debug.LogError("OnPingTimeOut: lastPongTime: "+ (Time.realtimeSinceStartup - lastPongTime));
                onDisconnectFunction?.Invoke();
                isConnected = false;
                client.ForceToReconnect();
            }
        }
        public bool IsConnected()
        {
            return isConnected;
        }
        #endregion
        void WriteToConsole(string str)
        {
            if (debugLevel == DEBUG_LEVEL.ON)
            {
                Debug.Log(str);
            }
        }
    }
}
