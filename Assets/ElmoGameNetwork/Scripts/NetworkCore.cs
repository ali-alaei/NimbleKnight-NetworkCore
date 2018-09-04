using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace ElmoGameNetwork

{
    public class NetworkCore : MonoBehaviour
    {

  

        #region ATTRIBUTES

        IConnection connection;
        ConnectionFactory factory;
        IModel channel_Received;
        IModel channel_Send;
        string queueName;
        string routingKey;
        IBasicProperties props;

        public bool debugMode;
        public string RoutingKey
        {
            get {
                return routingKey;
            }
        }
        #endregion

        #region EVENTS

        public Action OnConnected;
        public Action OnDesconnected;
        public Action OnSubscribe;
        public Action<string> OnMessageReceived;
        public Action OnCouldNotConnect;


        #endregion

        #region PUBLIC API

        private void Start()
        {

        }
        public void Connect()
        {
            CreateFactory();
            CreateConnection();
        }
        public void ForceToReconnect()
        {
            CreateConnection();
        }
        public void PublishMessage(string message)
        {
            try
            {
                if(debugMode)
                    Debug.Log("PublishMessage: " + message);
                byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(message);

                props.Headers["hmac"]= Encryption.GetHashString(messageBodyBytes, NetworkConfig.RabbitPassword);

                channel_Send.BasicPublish(NetworkConfig.OUT_Exchange, NetworkConfig.ServerRoutingKey, props, messageBodyBytes);

            }
            catch(Exception ex)
            {
                Debug.LogError("PublishMessage: " + ex.ToString());
            }
        }
        #endregion

        #region INTERNAL
        private void OnConnect()
        {
            Debug.Log("ElmoGameNetwork: " + "Connected Successfully ");

            connection.CallbackException += Connection_CallbackException;
            connection.ConnectionBlocked += Connection_ConnectionBlocked;
            connection.ConnectionRecoveryError += Connection_ConnectionRecoveryError;
            connection.ConnectionShutdown += Connection_ConnectionShutdown;
            connection.ConnectionUnblocked += Connection_ConnectionUnblocked;
            connection.RecoverySucceeded += Connection_RecoverySucceeded;
            DoMainThread.ExecuteOnExit(() => UnsubscribeFromEvents());
            DoMainThread.ExecuteOnMainThread(() => {
                OnConnected?.Invoke();
            });
            Subscribe();
        }
        private void CreateFactory()
        {
            try
            {
                factory = new ConnectionFactory();
                factory.UserName = NetworkConfig.RabbitUsername;
                factory.Password = NetworkConfig.RabbitPassword;
                factory.VirtualHost = NetworkConfig.VirtualHost;
                factory.HostName = NetworkConfig.HostName;
                factory.Port = NetworkConfig.Port;
                factory.AutomaticRecoveryEnabled = true;
                factory.TopologyRecoveryEnabled = false;
                factory.RequestedHeartbeat = 40;
                
            }
            catch(Exception ex)
            {
                Debug.LogError("CreateFactory Expection" + ex.ToString());
            }
        }
        async private void CreateConnection()
        {
            await Task.Run(() => {
                try
                {
                    Debug.Log("ElmoGameNetwork: Try To Create Connection...");

                    CloseConnection();
                    connection = factory.CreateConnection();
                }
                catch(Exception ex)
                {
                    Debug.Log("Couldent Connect to the Network: " + ex.ToString());

                    DoMainThread.ExecuteOnMainThread(() => OnCouldNotConnect?.Invoke());
                    DoMainThread.ExecuteOnMainThread(() =>
                    MyTime.IInvoke(() => {
                        CreateConnection();
                    }, 5));
                }

            });
            if(connection != null && connection.IsOpen)
                OnConnect();
        }
        private void Subscribe()
        {
            try
            {
                channel_Received = connection.CreateModel();
                channel_Send = connection.CreateModel();

                routingKey = NetworkConfig.UUID;
                queueName = NetworkConfig.QueuePrefix + routingKey;

                channel_Received.QueueDeclare(queueName, false, false, true, null);
                channel_Received.QueueBind(queueName, NetworkConfig.IN_Exchange, routingKey, null);
                props = channel_Received.CreateBasicProperties();
                props.Headers = new Dictionary<string, object>();
                props.ReplyTo = routingKey;
                props.Headers.Add("user_id", NetworkConfig.UserID);
                props.Headers.Add("hmac", "null");
                var consumer = new EventingBasicConsumer(channel_Received);
                consumer.Received += (ch, ea) => {
                    var body = ea.Body;
                    channel_Received.BasicAck(ea.DeliveryTag, false);
                    string text = System.Text.Encoding.UTF8.GetString(body);
                    OnMessageRecived(text);
                };
                string consumerTag = channel_Received.BasicConsume(queueName, false, consumer);
                Debug.Log("ElmoGameNetwork: " + "Subscribe Successfully ");
                DoMainThread.ExecuteOnMainThread(() => {
                    OnSubscribe?.Invoke();
                });
                connection.AutoClose = true;

            }
            catch(System.Exception ex)
            {
                ForceToReconnect();

                Debug.LogError(ex.ToString());
            }

        }
        private void UnsubscribeFromEvents()
        {
            connection.CallbackException -= Connection_CallbackException;
            connection.ConnectionBlocked -= Connection_ConnectionBlocked;
            connection.ConnectionRecoveryError -= Connection_ConnectionRecoveryError;
            connection.ConnectionShutdown -= Connection_ConnectionShutdown;
            connection.ConnectionUnblocked -= Connection_ConnectionUnblocked;
            connection.RecoverySucceeded -= Connection_RecoverySucceeded;

            OnConnected = null;
            OnDesconnected = null;
            OnSubscribe = null;
            OnMessageReceived = null;
        }
        void CloseConnection()
        {

            // Debug.Log("ClosingConnection ");
            try
            {
                if(channel_Received != null)
                {
                    channel_Received.Close();
                    channel_Received.Dispose();
                    channel_Send.Close();
                    channel_Send.Dispose();
                    Debug.Log("Channel Closed: ");
                }
            }
            catch(System.Exception ex)
            {
                Debug.Log("exeption in closing channel: " + ex.ToString());

            }
            try
            {
                if(connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                    Debug.Log("Connection disposed: ");
                }
            }
            catch(System.Exception ex)
            {
                Debug.Log("exeption in closing connection: " + ex.ToString());
            }
        }
        #endregion

        #region EVENTS
        private void OnMessageRecived(string message)
        {
            if (debugMode)
            Debug.Log("ElmoGameNetwork: OnMessageRecived: " + message);
            DoMainThread.ExecuteOnMainThread(() => {
                OnMessageReceived?.Invoke(message);
            }
            );
        }
        private void Connection_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            Debug.Log("ElmoGameNetwork: Connection_CallbackException" + e.ToString());
        }

        private void Connection_ConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs e)
        {
            Debug.Log("ElmoGameNetwork: Try To Recover...");
        }

        private void Connection_ConnectionUnblocked(object sender, EventArgs e)
        {
            Debug.Log("ElmoGameNetwork: Connection_ConnectionUnblocked" + e.ToString());
        }

        private void Connection_ConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            Debug.Log("ElmoGameNetwork: Connection_ConnectionBlocked" + e.ToString());
        }

        private void Connection_RecoverySucceeded(object sender, EventArgs e)
        {
            try
            {
                Debug.Log("ElmoGameNetwork: Reconnect Successfully");
                DoMainThread.ExecuteOnMainThread(() => CreateConnection());
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);
            }

        }
        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Debug.Log("ElmoGameNetwork: Disconnected: " + e.ToString());
            DoMainThread.ExecuteOnMainThread(() => {
                OnDesconnected?.Invoke();
            });
           // ForceToReconnect();
        }

        private void OnDestroy()
        {
            CloseConnection();
        }
        #endregion
    }

}

// Update is called once per frame

