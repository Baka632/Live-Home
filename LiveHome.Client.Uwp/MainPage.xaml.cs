using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LiveHome.Client.Uwp
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private Client Client;
        private double _temperature;
        private double _relativeHumidity;
        private double _heatIndex;
        private bool _isCombustibleGasDetected;
        private string _serviceUri;
        private string _infoBarTitle;
        private bool _isInfoBarOpen;
        private string _infoBarMessage;
        private InfoBarSeverity _infoBarSeverity;
        private bool _isServiceControlEnabled = true;
        private Visibility _serviceInfoControlVisibility = Visibility.Collapsed;
        private bool _isGettingInfo;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public double HeatIndex
        {
            get => _heatIndex;
            set
            {
                _heatIndex = value;
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

        public Visibility ServiceInfoControlVisibility
        {
            get => _serviceInfoControlVisibility;
            set
            {
                _serviceInfoControlVisibility = value;
                OnPropertiesChanged();
            }
        }

        private async void Update(object sender, RoutedEventArgs e)
        {
            try
            {
                IsGettingInfo = true;
                IsServiceControlEnabled = false;
                if (IsInfoBarOpen)
                {
                    IsInfoBarOpen = false;
                }

                if (string.IsNullOrEmpty(ServiceUri))
                {
                    ShowInfoBar("无效的服务地址", "请检查你输入的值。", InfoBarSeverity.Warning);
                    return;
                }

                if (Client == null)
                {
                    Client = new Client(ServiceUri, new HttpClient());
                }
                else
                {
                    Client.BaseUrl = ServiceUri;
                }

                EnvironmentInfo envInfo = await Client.EnvironmentInfoAsync();
                Temperature = envInfo.Temperature;
                RelativeHumidity = envInfo.RelativeHumidity;
                HeatIndex = envInfo.HeatIndex;
                IsCombustibleGasDetected = await Client.CombustibleGasInfoAsync();
                ServiceInfoControlVisibility = Visibility.Visible;
            }
            catch (InvalidOperationException)
            {
                ShowInfoBar("无效的服务地址", "请检查你输入的值。", InfoBarSeverity.Error);
                ServiceInfoControlVisibility = Visibility.Collapsed;
            }
            catch (ApiException ex)
            {
                ShowInfoBar("无效的服务器响应", $"请检查是否输入本服务的地址。\n详细信息:\n{ex.Message}", InfoBarSeverity.Error);
                ServiceInfoControlVisibility = Visibility.Collapsed;
            }
            catch (HttpRequestException ex)
            {
                ShowInfoBar("无法与服务器建立联系", $"请检查服务是否打开,以及是否可以连接到Internet。\n详细信息:\n{ex.Message}", InfoBarSeverity.Error);
                ServiceInfoControlVisibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowInfoBar("未知错误", $"详细信息:\n{ex.Message}", InfoBarSeverity.Error);
                ServiceInfoControlVisibility = Visibility.Collapsed;
            }
            finally
            {
                IsServiceControlEnabled = true;
                IsGettingInfo = false;
            }
        }

        private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
        {
            InfoBarTitle = title;
            InfoBarMessage = message;
            InfoBarSeverity = severity;
            if (IsInfoBarOpen != true)
            {
                IsInfoBarOpen = true;
            }
        }

        public string InfoBarTitle
        {
            get => _infoBarTitle;
            set
            {
                _infoBarTitle = value;
                OnPropertiesChanged();
            }
        }

        public string InfoBarMessage
        {
            get => _infoBarMessage;
            set
            {
                _infoBarMessage = value;
                OnPropertiesChanged();
            }
        }

        public bool IsInfoBarOpen
        {
            get => _isInfoBarOpen;
            set
            {
                _isInfoBarOpen = value;
                OnPropertiesChanged();
            }
        }

        public InfoBarSeverity InfoBarSeverity
        {
            get => _infoBarSeverity;
            set
            {
                _infoBarSeverity = value;
                OnPropertiesChanged();
            }
        }
    }
}
