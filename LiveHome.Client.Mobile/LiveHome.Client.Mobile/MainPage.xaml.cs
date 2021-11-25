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
        public new event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async Task UpdateInfo()
        {
            //try
            //{
            //    IsGettingInfo = true;
            //    IsServiceControlEnabled = false;

            //    if (string.IsNullOrEmpty(ServiceUri))
            //    {
            //        ShowInfoBar("没有输入服务地址", "请输入服务地址");
            //        return;
            //    }

            //    Client.BaseUrl = ServiceUri;

            //    IsCombustibleGasDetected = await Client.CombustibleGasInfoAsync();
            //    gasLabel.Text = IsCombustibleGasDetected ? "是" : "否";
            //    EnvironmentInfo envInfo = await Client.EnvironmentInfoAsync();
            //    Temperature = envInfo.Temperature;
            //    tempLable.Text = envInfo.Temperature.ToString("0.#");
            //    RelativeHumidity = envInfo.RelativeHumidity;
            //    rhLable.Text = envInfo.RelativeHumidity.ToString();
            //    lastCheckTimeLable.Text = DateTimeOffset.Now.ToString();
            //    if (IsCombustibleGasDetected)
            //    {
            //        ShowGasWarning?.Invoke();
            //    }
            //}
            //catch (InvalidOperationException)
            //{
            //    ShowInfoBar("无效的服务地址", "请检查你输入的值。");
            //    ServiceInfoControlVisibility = false;
            //}
            //catch (ApiException ex)
            //{
            //    ShowInfoBar("无效的服务器响应", $"请检查是否输入本服务的地址。\n详细信息:\n{ex.Message}");
            //    ServiceInfoControlVisibility = false;
            //}
            //catch (HttpRequestException ex)
            //{
            //    ShowInfoBar("无法与服务器建立联系", $"请检查服务是否打开,以及是否可以连接到Internet。\n详细信息:\n{ex.Message}");
            //    ServiceInfoControlVisibility = false;
            //}
            //catch (Exception ex)
            //{
            //    ShowInfoBar("未知错误", $"详细信息:\n{ex.Message}");
            //    ServiceInfoControlVisibility = false;
            //}
            //finally
            //{
            //    LastCheckTime = DateTimeOffset.Now;
            //    IsServiceControlEnabled = true;
            //    IsGettingInfo = false;
            //}
        }

        private async void ShowInfoBar(string title, string message)
        {
            await DisplayAlert(title, message, "了解");
        }

        private void ConnectServer(object sender, EventArgs e)
        {
            ViewModel.ConnectServer();
        }

        private void DisconnectServer(object sender, EventArgs e)
        {
            ViewModel.DisconnectServer();
        }
    }
}
