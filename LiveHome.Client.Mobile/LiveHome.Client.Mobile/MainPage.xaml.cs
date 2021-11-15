using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LiveHome.Client.Mobile.WebApi;
using Xamarin.Forms;

namespace LiveHome.Client.Mobile
{
    public partial class MainPage : ContentPage
    {
        private WebApiClient Client = new WebApiClient(null, new HttpClient());

        public MainPage()
        {
            this.InitializeComponent();
            Device.StartTimer(new TimeSpan(0, 0, 30), () => Timer_Tick());
        }

        private bool Timer_Tick()
        {
            if (IsGettingInfo)
            {
                return true;
            }
            UpdateInfo();
            return IsTimerEnabled;
        }

        public static readonly BindableProperty _IsTimerEnabled =
            BindableProperty.Create("IsTimerEnabled", typeof(bool), typeof(MainPage), false);

        public bool IsTimerEnabled
        {
            get => (bool)GetValue(_IsTimerEnabled);
            set => SetValue(_IsTimerEnabled, value);
        }

        public static readonly BindableProperty _LastCheckTime =
            BindableProperty.Create("LastCheckTime", typeof(DateTimeOffset), typeof(MainPage), DateTimeOffset.Now);

        public DateTimeOffset LastCheckTime
        {
            get => (DateTimeOffset)GetValue(_LastCheckTime);
            set => SetValue(_LastCheckTime, value);
        }

        public static readonly BindableProperty _Temperature =
            BindableProperty.Create("Temperature", typeof(double), typeof(MainPage), 0d);
        public double Temperature
        {
            get => (double)GetValue(_Temperature);
            set => SetValue(_Temperature, value);
        }

        public static readonly BindableProperty _RelativeHumidity =
            BindableProperty.Create("RelativeHumidity", typeof(double), typeof(MainPage), 0d);
        public double RelativeHumidity
        {
            get => (double)GetValue(_RelativeHumidity);
            set => SetValue(_RelativeHumidity, value);
        }

        public static readonly BindableProperty _IsCombustibleGasDetected =
            BindableProperty.Create("IsCombustibleGasDetected", typeof(bool), typeof(MainPage), false);

        public bool IsCombustibleGasDetected
        {
            get => (bool)GetValue(_IsCombustibleGasDetected);
            set => SetValue(_IsCombustibleGasDetected, value);
        }

        public static readonly BindableProperty _ServiceUri =
            BindableProperty.Create("ServiceUri", typeof(string), typeof(MainPage), "");

        public string ServiceUri
        {
            get => (string)GetValue(_ServiceUri);
            set => SetValue(_ServiceUri, value);
        }

        public static readonly BindableProperty _IsServiceControlEnabled =
            BindableProperty.Create("IsServiceControlEnabled", typeof(bool), typeof(MainPage), true);

        public bool IsServiceControlEnabled
        {
            get => (bool)GetValue(_IsServiceControlEnabled);
            set => SetValue(_IsServiceControlEnabled, value);
        }

        public static readonly BindableProperty _IsGettingInfo =
            BindableProperty.Create("IsGettingInfo", typeof(bool), typeof(MainPage), false);

        public bool IsGettingInfo
        {
            get => (bool)GetValue(_IsGettingInfo);
            set => SetValue(_IsGettingInfo, value);
        }

        public static readonly BindableProperty _ServiceInfoControlVisibility =
            BindableProperty.Create("ServiceInfoControlVisibility", typeof(bool), typeof(MainPage), false);

        public bool ServiceInfoControlVisibility
        {
            get => (bool)GetValue(_ServiceInfoControlVisibility);
            set => SetValue(_ServiceInfoControlVisibility, value);
        }

        private async void UpdateEnvInfo(object sender, EventArgs e)
        {
            if (IsGettingInfo)
            {
                return;
            }
            await UpdateInfo();
        }

        private async Task UpdateInfo()
        {
            try
            {
                IsGettingInfo = true;
                IsServiceControlEnabled = false;

                if (string.IsNullOrEmpty(ServiceUri))
                {
                    return;
                }

                Client.BaseUrl = ServiceUri;

                IsCombustibleGasDetected = await Client.CombustibleGasInfoAsync();
                EnvironmentInfo envInfo = await Client.EnvironmentInfoAsync();
                Temperature = envInfo.Temperature;
                RelativeHumidity = envInfo.RelativeHumidity;
                if (IsCombustibleGasDetected)
                {
                    ShowGasWarning();
                }
            }
            catch (InvalidOperationException)
            {
                ShowInfoBar("无效的服务地址", "请检查你输入的值。");
                ServiceInfoControlVisibility = false;
            }
            catch (ApiException ex)
            {
                ShowInfoBar("无效的服务器响应", $"请检查是否输入本服务的地址。\n详细信息:\n{ex.Message}");
                ServiceInfoControlVisibility = false;
            }
            catch (HttpRequestException ex)
            {
                ShowInfoBar("无法与服务器建立联系", $"请检查服务是否打开,以及是否可以连接到Internet。\n详细信息:\n{ex.Message}");
                ServiceInfoControlVisibility = false;
            }
            catch (Exception ex)
            {
                ShowInfoBar("未知错误", $"详细信息:\n{ex.Message}");
                ServiceInfoControlVisibility = false;
            }
            finally
            {
                LastCheckTime = DateTimeOffset.Now;
                IsServiceControlEnabled = true;
                IsGettingInfo = false;
            }
        }

        private void ShowInfoBar(string v1, string v2)
        {
            throw new NotImplementedException();
        }

        private void ShowGasWarning()
        {
            throw new NotImplementedException();
        }
    }
}
