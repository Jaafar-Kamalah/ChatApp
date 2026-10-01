using ChatApp.MVVM;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ChatApp.Models
{
    class ServerManager : PropertyChangedBase
    {
        //Variables
        private bool listening;

        public bool Listening
        {
            get { return listening; }
            set 
            { 
                listening = value;
                OnPropertyChanged();
            }
        }

        private bool serverIsChatting;

        public bool ServerIsChatting
        {
            get { return serverIsChatting; }
            set 
            { 
                serverIsChatting = value;
                OnPropertyChanged();
            }
        }

        private string newMessage;

        public string NewMessage
        {
            get { return newMessage; }
            set
            {
                newMessage = value;
                OnPropertyChanged();
            }
        }

        private bool clearChatt;

        public bool ClearChatt
        {
            get { return clearChatt; }
            set 
            { 
                clearChatt = value;
                OnPropertyChanged();
            }
        }

        private string establishConnection;

        public string EstablishConnection
        {
            get { return establishConnection; }
            set 
            { 
                establishConnection = value; 
                OnPropertyChanged();
            }
        }

        private string messageBox;

        public string MessageBox
        {
            get { return messageBox; }
            set 
            { 
                messageBox = value;
                OnPropertyChanged();
            }
        }

        private bool buzzed;

        public bool Buzzed
        {
            get { return buzzed; }
            set
            {
                buzzed = value;
                OnPropertyChanged();
            }
        }

        private Socket listeningSocket, communicationSocket, newCommunicationSocket;
        private byte[] buffer;
        private string serverName;
        private DataHandler dataHandler;

        //Functions
        public ServerManager(DataHandler dh)
        {
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            communicationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            newCommunicationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            buffer = new byte[communicationSocket.ReceiveBufferSize];
            serverName = "";
            serverIsChatting = false;
            newMessage = "";
            establishConnection = "";
            messageBox = "";
            dataHandler = dh;
        }

        public void StartListening(string name, int port)
        {
            try
            {
                Listening = true;
                serverName = name;
                listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listeningSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                listeningSocket.Listen(0);
                listeningSocket.BeginAccept(new AsyncCallback(AcceptClientsCallback), null);
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed in initialization phase!\n" + e.Message;
                StopListening();
            }
        }

        private void AcceptClientsCallback(IAsyncResult ar)
        {
            try
            {
                if(listening)
                {
                    if (serverIsChatting)
                    {
                        newCommunicationSocket = listeningSocket.EndAccept(ar);
                        MessageRequest establishRequest = new MessageRequest("connectionbusy", serverName, "");
                        string establishRequestJSON = JsonSerializer.Serialize(establishRequest);
                        byte[] data = System.Text.Encoding.UTF8.GetBytes(establishRequestJSON);
                        newCommunicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendBusyCallback), null);
                    }
                    else
                    {
                        communicationSocket = listeningSocket.EndAccept(ar);
                        listeningSocket.BeginAccept(new AsyncCallback(AcceptClientsCallback), null); // Allow multiple requests
                        communicationSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveRequestCallback), null);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed in acception phase! \n" + e.Message;
                StopListening();
            }
        }

        private void SendBusyCallback(IAsyncResult ar)
        {
            try
            {
                int bytesSent = newCommunicationSocket.EndSend(ar);
                listeningSocket.BeginAccept(new AsyncCallback(AcceptClientsCallback), null); // Allow multiple requests
            }
            catch (Exception e)
            {
                MessageBox = "Failed to send busy notice: " + e.Message;
            }
        }

        private void ReceiveRequestCallback(IAsyncResult ar)
        {
            try
            {
                if (listening)
                {
                    int bytesRead = communicationSocket.EndReceive(ar);
                    string requestJSON = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\0');
                    MessageRequest? request = JsonSerializer.Deserialize<MessageRequest>(requestJSON);
                    if (request != null && request.RequestType == "establishconnection")
                    {
                        EstablishConnection = request.Name;

                        
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed in receiving phase! \n" + e.Message;
                StopListening();
            }
        }

        public void SendReply(bool result, string clientname)
        {
            MessageRequest reply = new MessageRequest("establishconnectionreply", serverName, "");
            if (result)
            {
                reply.Message = "accepted";
                ServerIsChatting = true;
                dataHandler.NewChat(clientname);
            }
            else
            {
                reply.Message = "declined";
            }

            string replyJSON = JsonSerializer.Serialize(reply);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(replyJSON);
            communicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendReplyCallback), null);
        }

        private void SendReplyCallback(IAsyncResult ar)
        {
            try
            {
                if (listening)
                {
                    communicationSocket.EndSend(ar);
                    if(ServerIsChatting)
                    {
                        communicationSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessagesCallback), null);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed while sending reply:\n" + e.Message;
                StopListening();
            }
        }

        private void ReceiveMessagesCallback(IAsyncResult ar)
        {
            try
            {
                if (listening)
                {
                    int bytesRead = communicationSocket.EndReceive(ar);


                    string requestJSON = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\0');
                    MessageRequest? reply = JsonSerializer.Deserialize<MessageRequest>(requestJSON);

                    if (reply != null)
                    {
                        if (reply.RequestType == "closeconnection")
                        {
                            MessageBox = reply.Name + " has disconnected.";
                            ServerIsChatting = false;
                            ClearChatt = true;
                            ClearChatt = false;
                            return;
                        }
                        else if (reply.RequestType == "message")
                        {
                            NewMessage = reply.Name + " : " + reply.Message;
                            dataHandler.NewMessage(reply);
                        }
                        else if (reply.RequestType == "buzz")
                        {
                            Buzzed = true;
                            Buzzed = false;
                        }
                    }

                    // Start the next asynchronous receive operation
                    communicationSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessagesCallback), null);
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    MessageBox = "Connection forcibly closed by the remote host.";
                    ServerIsChatting = false;
                    communicationSocket.Close();
                }
                else
                {
                    MessageBox = "Connection failed in communication phase due to socket error:\n" + se.Message;
                    StopListening();
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed in communication phase:\n" + e.Message;
                StopListening();
            }
        }

        public void Send(string message)
        {
            try
            {
                if (listening)
                {
                    MessageRequest establishRequest = new MessageRequest("message", serverName, message);
                    dataHandler.NewMessage(establishRequest);
                    string establishRequestJSON = JsonSerializer.Serialize(establishRequest);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(establishRequestJSON);
                    communicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendMessageCallback), null);
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed while sending a message:\n" + e.Message;
                StopListening();
            }
        }

        public void Buzz()
        {
            try
            {
                if (listening)
                {
                    MessageRequest establishRequest = new MessageRequest("buzz", serverName, "");
                    string establishRequestJSON = JsonSerializer.Serialize(establishRequest);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(establishRequestJSON);
                    communicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendMessageCallback), null);
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed while buzzing:\n" + e.Message;
                StopListening();
            }
        }

        private void SendMessageCallback(IAsyncResult ar)
        {
            communicationSocket.EndSend(ar);
        }

        public void StopListening()
        {
            try
            {
                Listening = false;
                if (serverIsChatting)
                {
                    
                    SendDisconnection();
                    ClearChatt = true;
                    ClearChatt = false;
                }
                else
                {
                    communicationSocket.Close();
                }
                listeningSocket.Close();
                newCommunicationSocket.Close();
                
            }
            catch (Exception e)
            {
                MessageBox = "Failed to close sockets: " + e.Message;

            }
        }

        private void SendDisconnection()
        {
            MessageRequest establishRequest = new MessageRequest("closeconnection", serverName, "");
            string establishRequestJSON = JsonSerializer.Serialize(establishRequest);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(establishRequestJSON);
            communicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendDisconnectionCallback), null);
        }

        private void SendDisconnectionCallback(IAsyncResult ar)
        {
            try
            {
                int bytesSent = communicationSocket.EndSend(ar);
                ServerIsChatting = false;
                communicationSocket.Close();
            }
            catch (Exception e)
            {
                MessageBox = "Failed to send disconnection notice: " + e.Message;
            }
        }
    }
}
