using ChatApp.MVVM;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace ChatApp.Models
{
    class HistoryManager : PropertyChangedBase
    {
         
        private ObservableCollection<HistoryTab> historyDatabase;

        public ObservableCollection<HistoryTab> HistoryDatabase
        {
            get { return historyDatabase; }
            set 
            { 
                historyDatabase = value;
                OnPropertyChanged();
            }
        }

        private DataHandler datahandler;

        public HistoryManager(DataHandler dh)
        {
            datahandler = dh; 
            HistoryDatabase =
            new ObservableCollection<HistoryTab>();
        }
        public ObservableCollection<MessageRequest> ViewChatt(HistoryTab selectedHistoryTab)
        {
            return JsonConvert.DeserializeObject<ObservableCollection<MessageRequest>>(datahandler.GetMessageFile(selectedHistoryTab.FileName));
        }

        public void UpdateHistory()
        {
            HistoryDatabase = JsonConvert.DeserializeObject<ObservableCollection<HistoryTab>>(datahandler.GetHistoryIndex());
        }
    }
}