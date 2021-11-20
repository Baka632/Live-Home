using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LiveHome.Client.Mobile.WebApi;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace LiveHome.Client.Mobile
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly WebApiClient Client = new WebApiClient(null, new HttpClient());
        private bool _isTimerEnabled;
        private DateTimeOffset _lastCheckTime;
        private double _temperature;
        private double _relativeHumidity;
        private bool _isCombustibleGasDetected;
        private string _serviceUri;
        private bool _isServiceControlEnabled;
        private bool _isGettingInfo;
        private bool _serviceInfoControlVisibility;
        public static Action ShowGasWarning = null;

        public new event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 通知系统属性已经发生更改
        /// </summary>
        /// <param name="propertyName">发生更改的属性名称,其填充是自动完成的</param>
        public void OnPropertiesChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool Timer_Tick()
        {
            if (IsGettingInfo)
            {
                return true;
            }
            Task task = new Task(async () =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () => await UpdateInfo());
            });
            task.Start();
            return IsTimerEnabled;
        }


        public bool IsTimerEnabled
        {
            get => _isTimerEnabled;
            set
            {
                _isTimerEnabled = value;
                if (value)
                {
                    Device.StartTimer(new TimeSpan(0, 0, 30), () => Timer_Tick());
                }
                OnPropertiesChanged();
            }
        }

        public DateTimeOffset LastCheckTime
        {
            get => _lastCheckTime;
            set
            {
                _lastCheckTime = value;
                OnPropertiesChanged();
            }
        }

        public double Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                OnPropertiesChanged();
            }
        }

        public double RelativeHumidity
        {
            get => _relativeHumidity;
            set
            {
                _relativeHumidity = value;
                OnPropertiesChanged();
            }
        }

        public bool IsCombustibleGasDetected
        {
            get => _isCombustibleGasDetected;
            set
            {
                _isCombustibleGasDetected = value;
                OnPropertiesChanged();
            }
        }

        public string ServiceUri
        {
            get => _serviceUri;
            set
            {
                _serviceUri = value;
                OnPropertiesChanged();
            }
        }

        public bool IsServiceControlEnabled
        {
            get => _isServiceControlEnabled;
            set
            {
                _isServiceControlEnabled = value;
                OnPropertiesChanged();
            }
        }

        public bool IsGettingInfo
        {
            get => _isGettingInfo;
            set
            {
                _isGettingInfo = value;
                OnPropertiesChanged();
            }
        }

        public bool ServiceInfoControlVisibility
        {
            get => _serviceInfoControlVisibility;
            set
            {
                _serviceInfoControlVisibility = value;
                OnPropertiesChanged();
            }
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
                    ShowInfoBar("没有输入服务地址", "请输入服务地址");
                    return;
                }

                Client.BaseUrl = ServiceUri;

                IsCombustibleGasDetected = await Client.CombustibleGasInfoAsync();
                gasLabel.Text = IsCombustibleGasDetected ? "是" : "否";
                EnvironmentInfo envInfo = await Client.EnvironmentInfoAsync();
                Temperature = envInfo.Temperature;
                tempLable.Text = envInfo.Temperature.ToString("0.#");
                RelativeHumidity = envInfo.RelativeHumidity;
                rhLable.Text = envInfo.RelativeHumidity.ToString();
                lastCheckTimeLable.Text = DateTimeOffset.Now.ToString();
                if (IsCombustibleGasDetected)
                {
                    ShowGasWarning?.Invoke();
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

        private async void ShowInfoBar(string title, string message)
        {
            await DisplayAlert(title, message, "了解");
        }

        private void ServiceUriChanged(object sender, TextChangedEventArgs e)
        {
            ServiceUri = e.NewTextValue;
        }

        private void OnCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            IsTimerEnabled = e.Value;
        }
    }
}
