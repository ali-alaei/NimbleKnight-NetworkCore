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
namespace ElmoGameNetwork
{


    public class NetworkManager : MonoBehaviour
    {
        #region Singleton Pattern

        static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get {
                if(_instance == null)
                    _instance = GameObject.FindObjectOfType<NetworkManager>();
                return _instance;
            }
        }

        #endregion

        #region ENUMS


        public enum RequestType
        {
            rest,
            non_rest,
            heartbeat,
        }
        public enum ConnectionType
        {
            dispatcher,
            worker
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
        #region ATTRIBUTES

        public NetworkCore clientPrefab;
        private NetworkCore client;
        private ConnectionType connectionType;



        #endregion

        #region  EVENTS


        #endregion

        void Awake()
        {
            connectionType = ConnectionType.dispatcher;
            NetworkConfig.SetupFromFile();
            EstablishNewConnection();

        }
        void EstablishNewConnection()
        {
            if(client != null)
                Destroy(client.gameObject);
            client = Instantiate(clientPrefab);
            client.OnMessageReceived = OnMessageReceived;
            client.OnDesconnected = OnDesconnected;
            client.OnSubscribe = OnSubscribe;
            client.Connect();
        }
        
        private void OnSubscribe()
        {
            switch(connectionType)
            {
                case ConnectionType.dispatcher:
                    SignUP();

                    break;
                case ConnectionType.worker:
                    JoinToNetwork();
                    break;
                default:
                    break;
            }
            //callback to upperClass
        }

        private void OnDesconnected()
        {
            //callback to upperClass

        }
        
        private void OnMessageReceived(string obj)
        {
            JSONObject result = new JSONObject(obj);
            string status = result.GetField("status").str;
            string response = result.GetField("response").str;
            JSONObject content = result.GetField("content");

//            if(response == "heartbeat")
//            {
//                HandleHeartBeat();
//            }
            if(response == "sign_up")
            {
                Debug.Log("in sign_up");
                print("result" + result.ToString());
                HandleNetworkDispatcher(content);
            }
            else if (response=="join")
            {
                print("result" + result.ToString());
                HandleJoin();
            }
        }

        private void HandleJoin()
        {

        }

        void SignUpAction(string username, string password)
        {
            //callback to upperClass
        }
        
        public void Login(string username, string password)
        {
            JSONObject args = new JSONObject();
            args.AddField("username", username);
            args.AddField("password", password);
            SendToServer(args, RequestType.rest, "login");
        }
        
        void JoinToNetwork()
        {
            SendToServer(null, RequestType.non_rest, "join");
        }
        
        public void SignUP()
        {
            SendToServer(null, RequestType.rest, "sign_up");
        }
        
        void HandleHeartBeat()
        {
            SendToServer(null, RequestType.heartbeat, "heartbeat");
        }
        void HandleNetworkDispatcher(JSONObject content)
        {
            string IP = content.GetField("ip").str;
            int Port = (int)content.GetField("port").n;
            string Out_exchange = content.GetField("in_exchange").str;
            string In_exchange = content.GetField("out_exchange").str;
            string In_queue = content.GetField("in_queue").str;
            string Uuid = content.GetField("uuid").str;
            string Login_Username = content.GetField("username").str;
            string Login_Password = content.GetField("password").str;
            string Username = NetworkConfig.RabbitUsername;
            string Password = NetworkConfig.RabbitPassword;
            string virtualHost = NetworkConfig.VirtualHost;
            string queuePrefix = NetworkConfig.QueuePrefix;
            JSONObject newNetworkConfig = new JSONObject();
            newNetworkConfig.AddField("RabbitUsername", Username);
            newNetworkConfig.AddField("RabbitPassword", Password);
            newNetworkConfig.AddField("VirtualHost", virtualHost);
            newNetworkConfig.AddField("HostName", IP);
            newNetworkConfig.AddField("Port", Port);
            newNetworkConfig.AddField("QueuePrefix", queuePrefix);
            newNetworkConfig.AddField("OUT_Exchange", Out_exchange);
            newNetworkConfig.AddField("IN_Exchange", In_exchange);
            newNetworkConfig.AddField("ServerRoutingKey", In_queue);
            newNetworkConfig.AddField("UUID", Uuid);
            newNetworkConfig.AddField("user_id", Login_Username);
            NetworkConfig.SetupFromJson(newNetworkConfig);
            connectionType = ConnectionType.worker;
            EstablishNewConnection();
            SignUpAction(Login_Username, Login_Password);
        }
        
        void SendToServer(JSONObject args, RequestType type, string request)
        {
            JSONObject message = new JSONObject();
            message.AddField("type", type.ToString());
            message.AddField("request", request);
            message.AddField("args", args);

            client.PublishMessage(message.ToString());
        }
        
        #region Messaging
        // Update is called once per frame


        #endregion





        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
            }
        }

    }
}
