using ChatApp.MVVM;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ChatApp.Models
{
    internal class ClientManager : PropertyChangedBase
    {
		//Variables
		private bool connected;

		public bool Connected
		{
			get { return connected; }
			set 
			{ 
				connected = value;
				OnPropertyChanged();
			}
		}

        private bool clientIsChatting;

        public bool ClientIsChatting
        {
            get { return clientIsChatting; }
            set 
            { 
                clientIsChatting = value; 
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


        private Socket communicationSocket;
        private byte[] buffer;
        private string clientName;
        private DataHandler dataHandler;

        //Functions
        public ClientManager(DataHandler dh)
        {
            communicationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            buffer = new byte[communicationSocket.ReceiveBufferSize];
            clientName = "";
            clientIsChatting = false;
            newMessage = "";
            messageBox = "";
            dataHandler = dh;
        }

        public void Connect(string name, string IP, int port)
		{
			try
			{
                Connected = true;
                clientName = name;
                communicationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                communicationSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), port), new AsyncCallback(ConnectCallback), null);
            }
			catch (Exception e) 
			{
                MessageBox = "Connection failed in initialization phase:\n" + e.Message;
                Disconnect();
            }
		}

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                if (connected)
                {
                    communicationSocket.EndConnect(ar);

                    // Send connection request
                    MessageRequest establishRequest = new MessageRequest("establishconnection", clientName, "");
                    string establishRequestJSON = JsonSerializer.Serialize(establishRequest);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(establishRequestJSON);
                    communicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendRequestCallback), null);
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    MessageBox = "Nobody with the specified IP is listening to this port";
                    Disconnect();
                }
                else
                {
                    MessageBox = "Connection failed in connecting phase due to socket error:\n" + se.Message;
                    Disconnect();
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed in connecting phase:\n" + e.Message;
                Disconnect();
            }
        }

        private void SendRequestCallback(IAsyncResult ar)
        {
            try
            {
                int bytesSent = communicationSocket.EndSend(ar);
                communicationSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveReplyCallback), null);

            }
            catch (Exception e)
            {
                MessageBox = "Connection failed while sending communication request:\n" + e.Message;
                Disconnect();
            }
        }

        private void ReceiveReplyCallback(IAsyncResult ar)
        {
            try
            {
                if (connected)
                {
                    int bytesRead = communicationSocket.EndReceive(ar);
                    string replyJSON = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\0');
                    MessageRequest? reply = JsonSerializer.Deserialize<MessageRequest>(replyJSON);
                    //MessageBox.Show(replyJSON, clientName);

                    if (reply != null && reply.RequestType == "establishconnectionreply")
                    {
                        if (reply.Message == "accepted")
                        {
                            ClientIsChatting = true;
                            MessageBox = reply.Name + " accepted your request!";
                            dataHandler.NewChat(reply.Name);
                        }
                        else
                        {
                            MessageBox = reply.Name + " declined your request.";
                            Disconnect();
                            return;
                        }
                    }
                    else if (reply != null && reply.RequestType == "connectionbusy")
                    {
                        MessageBox = reply.Name + " is currently chatting with another client, try again later.";
                        Disconnect();
                        return;
                    }
                    communicationSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessagesCallback), null);
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed while receiving communication reply:\n" + e.Message;
                Disconnect();
            }
        }

        private void ReceiveMessagesCallback(IAsyncResult ar)
        {
            try
            {
                if (connected)
                {
                    int bytesRead = communicationSocket.EndReceive(ar);


                    string requestJSON = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\0');
                    MessageRequest? reply = JsonSerializer.Deserialize<MessageRequest>(requestJSON);

                    if (reply != null)
                    {
                        if (reply.RequestType == "closeconnection")
                        {
                            ClearChatt = true;
                            ClearChatt = false;

                            MessageBox = reply.Name + " has disconnected.";
                            ClientIsChatting = false;
                            Disconnect();
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
                    ClientIsChatting = false;
                    Disconnect();
                }
                else
                {
                    MessageBox = "Connection failed in communication phase due to socket error:\n" + se.Message;
                    Disconnect();
                }
            }

            catch (Exception e)
            {
                MessageBox = "Connection failed in communication phase:\n" + e.Message;
                Disconnect();
            }
        }


        public void Send(string message)
        {
            try
            {
                if (connected)
                {
                    MessageRequest establishRequest = new MessageRequest("message", clientName, message);
                    dataHandler.NewMessage(establishRequest);
                    string establishRequestJSON = JsonSerializer.Serialize(establishRequest);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(establishRequestJSON);
                    communicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendMessageCallback), null);
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed while sending a message:\n" + e.Message;
                Disconnect();
            }
        }

        public void Buzz()
        {
            try
            {
                if (connected)
                {
                    MessageRequest establishRequest = new MessageRequest("buzz", clientName, "");
                    string establishRequestJSON = JsonSerializer.Serialize(establishRequest);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(establishRequestJSON);
                    communicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendMessageCallback), null);
                }
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed while buzzing:\n" + e.Message;
                Disconnect();
            }
        }

        private void SendMessageCallback(IAsyncResult ar)
        {
            communicationSocket.EndSend(ar);
        }

        public void Disconnect()
		{
            try
            {
                // Send a disconnection message if client was chatting
                if (clientIsChatting)
                {
                    MessageRequest establishRequest = new MessageRequest("closeconnection", clientName, "");
                    string establishRequestJSON = JsonSerializer.Serialize(establishRequest);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(establishRequestJSON);
                    communicationSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendDisconnectionCallback), null);

                    ClearChatt = true;
                    ClearChatt = false;
                }
                else
                {
                    Connected = false;
                    communicationSocket.Close();
                }
                
            }
            catch (Exception e)
            {
                MessageBox = "Connection in disconnection phase:\n" + e.Message;
            }
        }

        private void SendDisconnectionCallback(IAsyncResult ar)
        {
            try
            {
                int bytesSent = communicationSocket.EndSend(ar);
                ClientIsChatting = false;
                Connected = false;
                communicationSocket.Close();
            }
            catch (Exception e)
            {
                MessageBox = "Connection failed while sending disconnection notice:\n" + e.Message;
            }
        }
    }
}
