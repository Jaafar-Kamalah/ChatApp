using ChatApp.Models;
using ChatApp.ViewModels;
using System.Windows;

namespace ChatApp
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataHandler datahandler = new DataHandler();
            MainWindowViewModel viewModel = new MainWindowViewModel(new Models.HistoryManager(datahandler), new Models.ServerManager(datahandler), new Models.ClientManager(datahandler));
            DataContext = viewModel;
        }
    }
}