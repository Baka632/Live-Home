using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace LiveHome.Client.Mobile
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {      
        public MainPage()
        {
            this.InitializeComponent();
            ViewModel.ShowInfoBar = async (title, message) => await DisplayAlert(title, message, "了解");
        }

        private void ConnectServer(object sender, EventArgs e)
        {
            ViewModel.ConnectServer();
        }

        private void DisconnectServer(object sender, EventArgs e)
        {
            ViewModel.DisconnectServer();
        }

        private void GetDataNow(object sender, EventArgs e)
        {
            ViewModel.GetDataNow();
        }
    }
}
