using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ElmoGameNetwork
{
    public class NetworkConfig 
    {
        private static JSONObject configJson;
        private static bool isInitialize;
        public static string RabbitUsername
        {
            get
            {
                return configJson["RabbitUsername"].str;
            }
        }
       
        public static string RabbitPassword
        {
            get
            {
                return configJson["RabbitPassword"].str;
            }
        }
        public static string VirtualHost
        {
            get
            {
                return configJson["VirtualHost"].str;
            }
        }
        public static string HostName
        {
            get
            {
                return configJson["HostName"].str;
               
            }
        }
        public static int Port
        {
            get
            {
                return (int)(configJson["Port"].n);
            }
        }
        public static string QueuePrefix
        {
            get
            {
                return configJson["QueuePrefix"].str;
            }
        }
        public static string OUT_Exchange
        {
            get
            {
                return configJson["OUT_Exchange"].str;
            }
        }
        public static string IN_Exchange
        {
            get
            {
                return configJson["IN_Exchange"].str;
            }
        }
        public static string UUID
        {
            get {
                if (configJson.HasField("UUID"))
                {
                    return configJson["UUID"].str;
                }
                else
                {
                    return System.Guid.NewGuid().ToString();
                }
            }
        }
        public static string UserID
        {
            get {
                if(configJson.HasField("user_id"))
                {
                    return configJson["user_id"].str;
                }
                else
                {
                    return null;
                }
            }
        }
        public static string ServerRoutingKey
        {
            get
            {
                return configJson["ServerRoutingKey"].str;
            }
        }
        public static void SetupFromFile()
        {
            TextAsset config= Resources.Load<TextAsset>("NetworkConfiguration");
            configJson = new JSONObject(config.ToString());
        }
        public static void SetupFromJson(JSONObject config)
        {
            configJson = new JSONObject(config.ToString());

        }


    }
}
