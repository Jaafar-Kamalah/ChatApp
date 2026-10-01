using ChatApp.Models;
using ChatApp.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Threading;

namespace ChatApp.ViewModels
{
    class MainWindowViewModel : PropertyChangedBase
    {
        //////////////////////////////////Shared//////////////////////////////////
        //Variables
        private readonly Dispatcher uiDispatcher;

        //Functions
        public MainWindowViewModel(HistoryManager hm, ServerManager sm, ClientManager cm)
        {
            uiDispatcher = Application.Current.Dispatcher;

            //history
            historyManager = hm;
            History = hm.HistoryDatabase;
            hm.PropertyChanged += hm_PropertyChanged;
            hm.UpdateHistory();
            SelectedHistory = History;


            //server
            serverManager = sm;
            serverName = "Samuel";
            listeningPort = "12000";
            serverManager.PropertyChanged += serverManager_PropertyChanged;
            serverIsChatting = false;

            //client
            clientManager = cm;
            clientName = "Clint";
            connectIP = "127.0.0.1";
            connectPort = "12000";
            clientManager.PropertyChanged += clientManager_PropertyChanged;
            clientIsChatting = false;


            //chatt
            Messages = new ObservableCollection<string>();
            message = "";
            buzzer = new SoundPlayer("..\\..\\..\\..\\static\\buzz.wav");
        }

        //////////////////////////////////Server//////////////////////////////////
        //Variables
        private ServerManager serverManager;

        private string serverName;

        public string ServerName
        {
            get { return serverName; }
            set { serverName = value; }
        }

        private string listeningPort;

        public string ListeningPort
        {
            get { return listeningPort; }
            set { listeningPort = value; }
        }

        private bool listening;
        private bool serverIsChatting;

        //Commands
        public RelayCommand StartListeningCommand => new RelayCommand(execute => StartListening(), canExecute => !listening && int.TryParse(listeningPort, out int result) && serverName != "");
        public RelayCommand StopListeningCommand => new RelayCommand(execute => StopListening(), canExecute => listening);

        private void StartListening()
        {
            Messages.Clear();
            if (connected)
            {
                clientManager.Disconnect();
            }
            serverManager.StartListening(serverName, int.Parse(listeningPort));
        }

        private void StopListening()
        {
            serverManager.StopListening();
        }

        private void serverManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Listening")
            {
                listening = serverManager.Listening;
            }

            if (e.PropertyName == "ServerIsChatting")
            {
                serverIsChatting = serverManager.ServerIsChatting;
            }

            if (e.PropertyName == "NewMessage")
            {
                uiDispatcher.Invoke(() => { Messages.Add(serverManager.NewMessage); });
            }

            if (e.PropertyName == "ClearChatt" && serverManager.ClearChatt)
            {
                uiDispatcher.Invoke(() => { Messages.Clear(); });
                historyManager.UpdateHistory();
            }

            if (e.PropertyName == "EstablishConnection")
            { 
                MessageBoxResult result = MessageBox.Show("Chatt with " + serverManager.EstablishConnection + "?", serverName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    serverManager.SendReply(true, serverManager.EstablishConnection);
                }
                else
                {
                    serverManager.SendReply(false, "");
                }
            }

            if (e.PropertyName == "MessageBox")
            {
                MessageBox.Show(serverManager.MessageBox, serverName);
            }

            if (e.PropertyName == "Buzzed" && serverManager.Buzzed)
            {
                buzzer.Play();
            }
        }

        //////////////////////////////////Client//////////////////////////////////
        //Variables
        private ClientManager clientManager;

        private string clientName;

        public string ClientName
        {
            get { return clientName; }
            set { clientName = value; }
        }

        private string connectPort;

        public string ConnectPort
        {
            get { return connectPort; }
            set { connectPort = value; }
        }

        private string connectIP;

        public string ConnectIP
        {
            get { return connectIP; }
            set { connectIP = value; }
        }

        private bool connected;
        private bool clientIsChatting;

        //Commands
        public RelayCommand ConnectCommand => new RelayCommand(execute => Connect(), canExecute => !connected 
                                                                && int.TryParse(connectPort, out int result) && connectIP != "" && clientName != "");
        public RelayCommand DisconnectCommand => new RelayCommand(execute => Disconnect(), canExecute => connected);

        private void Connect()
        {
            Messages.Clear();
            if (listening)
            {
                serverManager.StopListening();
            }
            clientManager.Connect(clientName, connectIP, int.Parse(connectPort));
        }

        private void Disconnect()
        {
            clientManager.Disconnect();
        }

        private void clientManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Connected")
            {
                connected = clientManager.Connected;
            }

            if (e.PropertyName == "ClientIsChatting")
            {
                clientIsChatting = clientManager.ClientIsChatting;
            }

            if (e.PropertyName == "NewMessage")
            {
                uiDispatcher.Invoke(() => { Messages.Add(clientManager.NewMessage); });
            }

            if (e.PropertyName == "ClearChatt" && clientManager.ClearChatt)
            {
                uiDispatcher.Invoke(() => { Messages.Clear(); });
                historyManager.UpdateHistory();
            }

            if (e.PropertyName == "MessageBox")
            {
                MessageBox.Show(clientManager.MessageBox, clientName);
            }

            if (e.PropertyName == "Buzzed" && clientManager.Buzzed)
            {
                buzzer.Play();
            }
        }

        //////////////////////////////////History//////////////////////////////////
        //Variables
        private HistoryManager historyManager;

        private ObservableCollection<HistoryTab> history;
        public ObservableCollection<HistoryTab> History
        {
            get { return history; }
            set 
            { 
                history = value;          
                SelectedHistory = history;
            }
        }

        private ObservableCollection<HistoryTab> selectedHistory;
        public ObservableCollection<HistoryTab> SelectedHistory
        {
            get { return selectedHistory; }
            set
            {
                selectedHistory = value;
                OnPropertyChanged();
            }
        }


        private HistoryTab? selectedHistoryTab;

        public HistoryTab? SelectedHistoryTab
        {
            get { return selectedHistoryTab; }
            set { selectedHistoryTab = value; }
        }

        private string searchText;

        public string SearchText
        {
            get { return searchText; }
            set 
            {
                if (searchText != value)
                {
                    searchText = value;
                    if (searchText == "")
                    {
                        SelectedHistory = history;
                    }
                    else
                    {
                        IEnumerable<HistoryTab> query = from s in history
                                                        where s.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                                                        orderby s.Date
                                                        select s;
                        SelectedHistory = new ObservableCollection<HistoryTab>(query);
                    }
                }
                OnPropertyChanged();
            }
        }


        //Commands
        public RelayCommand ViewChattCommand => new RelayCommand(execute => ViewChatt(), canExecute => selectedHistoryTab != null);

        private void ViewChatt()
        {
            if (selectedHistoryTab != null)
            {
                if (connected)
                {
                    MessageBox.Show("Disconnect to view history");
                    return;
                }
                else if (listening)
                {
                    MessageBox.Show("Stop listening to view history");
                    return;
                }

                Messages.Clear();
                ObservableCollection<MessageRequest> unformattedMessages = historyManager.ViewChatt(selectedHistoryTab);
                for (int i = 0;  i < unformattedMessages.Count; i++)
                {
                    string currentMessage = unformattedMessages[i].Name + " : " + unformattedMessages[i].Message;
                    Messages.Add(currentMessage);
                }
            }
        }
        private void hm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        { 
            if(e.PropertyName == "HistoryDatabase")
            {
                History = historyManager.HistoryDatabase;
            }
        }
            
        //////////////////////////////////Chatt//////////////////////////////////
        //Variables
        public ObservableCollection<string> Messages { get; set; }

        private string message;

        public string Message
        {
            get { return message; }
            set 
            { 
                message = value; 
                OnPropertyChanged();
            }
        }

        SoundPlayer buzzer;

        //Commands
        public RelayCommand SendCommand => new RelayCommand(execute => Send(), canExecute => serverIsChatting || clientIsChatting);
        public RelayCommand BuzzCommand => new RelayCommand(execute => Buzz(), canExecute => serverIsChatting || clientIsChatting);

        private void Send()
        {
            string nameAndMessage = "";
            if (serverIsChatting)
            {
                serverManager.Send(message);
                nameAndMessage = serverName + " : " + message;
            }
            else if (clientIsChatting)
            {
                clientManager.Send(message);
                nameAndMessage = clientName + " : " + message;
            }
            Message = "";
            Messages.Add(nameAndMessage);
        }

        private void Buzz()
        {
            if (serverIsChatting)
            {
                serverManager.Buzz();
            }
            else if (clientIsChatting)
            {
                clientManager.Buzz();
            }
        }
    }
}
