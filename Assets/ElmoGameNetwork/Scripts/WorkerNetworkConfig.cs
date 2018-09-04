using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElmoGameNetwork
{
    public class WorkerNetworkConfig
    {

        public static string IP
        {
            get { return IP; }
            private set { IP = value; }
        }

        public static string Port
        {
            get { return Port; }
            private set { Port = value; }
        }
        public static string In_exchange
        {
            get { return In_exchange; }
            private set { In_exchange = value; }
        }

        public static string Out_exchange
        {
            get { return Out_exchange; }
            private set { Out_exchange = value; }
        }
        public static string In_queue
        {
            get { return In_queue; }
            private set { In_queue = value; }
        }
        public static string Uuid
        {
            get { return Uuid; }
            private set { Uuid = value; }
        }
        public static string Login_Username
        {
            get { return Login_Username; }
            private set { Login_Username = value; }
        }
        public static string Login_Password
        {
            get { return Login_Password; }
            private set { Login_Password = value; }
        }
        public static string Username
        {
            get { return Username; }
            private set { Username = value; }
        }
        public static string Password
        {
            get { return Password; }
            private set { Password = value; }
        }
        public static void Setup(JSONObject content)
        {
            IP = content.GetField("ip").ToString();
            Port = content.GetField("port").ToString();
            In_exchange = content.GetField("in_exchange").ToString();
            Out_exchange = content.GetField("out_exchange").ToString();
            In_queue = content.GetField("in_queue").ToString();
            Uuid = content.GetField("uuid").ToString();
            Login_Username = content.GetField("username").ToString();
            Login_Password = content.GetField("password").ToString();
            Username = NetworkConfig.RabbitUsername;
            Password = NetworkConfig.RabbitPassword;
            string virtualHost = NetworkConfig.VirtualHost;
            string queuePrefix = NetworkConfig.QueuePrefix;

            JSONObject newNetworkConfig = new JSONObject();
            newNetworkConfig.AddField("username", Username);
            newNetworkConfig.AddField("password", Password);
            newNetworkConfig.AddField("VirtualHost", virtualHost);
            newNetworkConfig.AddField("HostName", IP);
            newNetworkConfig.AddField("Port", Port);
            newNetworkConfig.AddField("QueuePrefix", queuePrefix);
            newNetworkConfig.AddField("OUT_Exchange", Out_exchange);
            newNetworkConfig.AddField("IN_Exchange", In_exchange);
            newNetworkConfig.AddField("ServerRoutingKey", In_queue);

        }
    }
}
