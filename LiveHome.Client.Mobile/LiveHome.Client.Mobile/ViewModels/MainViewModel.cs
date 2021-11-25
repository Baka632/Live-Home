using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Xamarin.Essentials;

namespace LiveHome.Client.Mobile
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private DateTimeOffset _lastCheckTime;
        private double _temperature;
        private double _relativeHumidity;
        private bool _isCombustibleGasDetected;
        private string _serviceUri;
        private bool _isServiceControlEnabled = true;
        private bool _isGettingInfo;
        private bool _serviceInfoControlVisibility;
        public static Action ShowGasWarning = null;
        public Action<string, string> ShowInfoBar = null;
        public HubConnection hubConnection;
        public event PropertyChangedEventHandler PropertyChanged;

        ~MainViewModel()
        {
            hubConnection?.StopAsync();
        }

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }

        /// <summary>
        /// 通知系统属性已经发生更改
        /// </summary>
        /// <param name="propertyName">发生更改的属性名称,其填充是自动完成的</param>
        public void OnPropertiesChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public async void GetDataNow()
        {
            try
            {
                IsServiceControlEnabled = false;

                if (hubConnection.State == HubConnectionState.Disconnected)
                {
                    ShowInfoBar("已断开连接", "与服务器的连接已经丢失");
                    IsServiceControlEnabled = true;
                    ServiceInfoControlVisibility = false;
                    return;
                }

                IsCombustibleGasDetected = await hubConnection.InvokeAsync<bool>("GetCombustibleGasInfo");
                string envInfoString = await hubConnection.InvokeAsync<string>("GetEnvironmentInfo");
                EnvironmentInfo envInfo = JsonSerializer.Deserialize<EnvironmentInfo>(envInfoString);
                Temperature = envInfo.Temperature;
                RelativeHumidity = envInfo.RelativeHumidity;
                ServiceInfoControlVisibility = true;
                if (IsCombustibleGasDetected)
                {
                    ShowGasWarning();
                }
            }
            catch (HttpRequestException ex)
            {
                ShowInfoBar("无法与服务器建立联系", $"请检查服务是否打开,以及是否可以连接到Internet。\n详细信息:\n{ex.Message}");
            }
            catch (HubException ex)
            {
                ShowInfoBar("服务器端出现问题", $"请稍等片刻,然后重试。\n详细信息:\n{ex.Message}");
            }
            catch (Exception ex)
            {
                ShowInfoBar("未知错误", $"详细信息:\n{ex.Message}");
            }
            finally
            {
                LastCheckTime = DateTimeOffset.Now;
                IsServiceControlEnabled = true;
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

        public async void ConnectServer()
        {

            if (string.IsNullOrEmpty(ServiceUri) || !Uri.TryCreate(ServiceUri, UriKind.Absolute, out _))
            {
                ShowInfoBar("无效的服务地址", "请检查你输入的值。");
                return;
            }

            IsServiceControlEnabled = false;

            if (hubConnection != null)
            {
                await hubConnection.StopAsync();
            }
            hubConnection = new HubConnectionBuilder().WithUrl(ServiceUri).WithAutomaticReconnect().Build();
            hubConnection.Closed += OnHubClosed;
            hubConnection.Reconnecting += OnHubReconnecting;
            hubConnection.Reconnected += OnHubReconnected;
            hubConnection.On<string>("ReceiveEnvironmentInfo", (message) => ReceiveEnvironmentInfo(message));
            hubConnection.On<bool>("ReceiveCombustibleGasInfo", (message) => ReceiveCombustibleGasInfo(message));
            try
            {
                await hubConnection.StartAsync();
                ServiceInfoControlVisibility = true;
            }
            catch (Exception ex)
            {
                ShowInfoBar("无法与服务器建立联系", $"请检查服务是否打开,以及是否可以连接到Internet。\n详细信息:\n{ex.Message}");
            }
            finally
            {
                IsServiceControlEnabled = true;
            }
        }

        public async void DisconnectServer()
        {
            await hubConnection.StopAsync();
        }

        public void ReceiveCombustibleGasInfo(bool message)
        {
            IsCombustibleGasDetected = message;
            if (IsCombustibleGasDetected)
            {
                ShowGasWarning?.Invoke();
            }
            LastCheckTime = DateTimeOffset.Now;
        }

        public void ReceiveEnvironmentInfo(string message)
        {
            EnvironmentInfo envInfo = JsonSerializer.Deserialize<EnvironmentInfo>(message);
            Temperature = envInfo.Temperature;
            RelativeHumidity = envInfo.RelativeHumidity;
            LastCheckTime = DateTimeOffset.Now;
        }

        private async Task OnHubReconnected(string arg)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsServiceControlEnabled = true;
                ShowInfoBar("已重新连接", null);
            });
        }

        private async Task OnHubReconnecting(Exception arg)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsServiceControlEnabled = false;
                if (arg is null)
                {
                    ShowInfoBar("正在重新连接...", null);
                }
                else
                {
                    ShowInfoBar("正在重新连接...", $"由于名为{arg.GetType().FullName}的错误,连接被迫断开");
                }
            });
        }

        private async Task OnHubClosed(Exception arg)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsServiceControlEnabled = true;
                ServiceInfoControlVisibility = false;
                if (arg is null)
                {
                    ShowInfoBar("已断开连接", null);
                }
                else
                {
                    ShowInfoBar("已断开连接", $"由于名为{arg.GetType().FullName}的错误,连接被迫断开");
                }
            });
        }
    }
}