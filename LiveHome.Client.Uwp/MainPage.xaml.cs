using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LiveHome.Client.Uwp
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private Client Client = new Client(null, new HttpClient());
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
        private DateTimeOffset _lastCheckTime;

        public event PropertyChangedEventHandler PropertyChanged;

        private DispatcherTimer timer = new DispatcherTimer();
        private bool _isTimerEnabled;

        public MainPage()
        {
            this.InitializeComponent();
            timer.Interval = new TimeSpan(0, 0, 30);
            timer.Tick += Timer_Tick;
            _isTimerEnabled = timer.IsEnabled;
        }

        private async void Timer_Tick(object sender, object e)
        {
            await UpdateInfo();
        }

        /// <summary>
        /// 通知系统属性已经发生更改
        /// </summary>
        /// <param name="propertyName">发生更改的属性名称,其填充是自动完成的</param>
        public void OnPropertiesChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsTimerEnabled
        {
            get => _isTimerEnabled;
            set
            {
                _isTimerEnabled = value;
                if (value)
                {
                    timer.Start();
                }
                else
                {
                    timer.Stop();
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

        private async void UpdateEnvInfo(object sender, RoutedEventArgs e)
        {
            await UpdateInfo();
        }

        private async Task UpdateInfo()
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

                Client.BaseUrl = ServiceUri;

                EnvironmentInfo envInfo = await Client.EnvironmentInfoAsync();
                Temperature = envInfo.Temperature;
                RelativeHumidity = envInfo.RelativeHumidity;
                HeatIndex = envInfo.HeatIndex;
                IsCombustibleGasDetected = await Client.CombustibleGasInfoAsync();
                ServiceInfoControlVisibility = Visibility.Visible;
                if (IsCombustibleGasDetected)
                {
                    ShowGasWarning();
                }
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
                LastCheckTime = DateTimeOffset.Now;
                IsServiceControlEnabled = true;
                IsGettingInfo = false;
            }
        }

        private void ShowGasWarning()
        {
            ToastContent toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "侦测到可燃气体"
                            },
                            new AdaptiveText()
                            {
                                Text = "请立即采取措施"
                            }
                        },
                        Attribution = new ToastGenericAttributionText()
                        {
                            Text = ServiceUri
                        }
                    }
                }
            };

            // Create the toast notification
            ToastNotification toastNotif = new ToastNotification(toastContent.GetXml());

            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
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

        private async void MailTo(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:stevemc123456@outlook.com"));
        }

        private async void GoToGithub(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/Baka632/Live-Home"));
        }
    }
}
