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
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LiveHome.Client.Uwp
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private HubConnection hubConnection;
        private double _temperature;
        private double _relativeHumidity;
        private bool _isCombustibleGasDetected;
        private string _serviceUri;
        private string _infoBarTitle;
        private bool _isInfoBarOpen;
        private string _infoBarMessage;
        private InfoBarSeverity _infoBarSeverity;
        private bool _isServiceControlEnabled = true;
        private Visibility _serviceInfoControlVisibility = Visibility.Collapsed;
        private DateTimeOffset _lastCheckTime;

        public event PropertyChangedEventHandler PropertyChanged;


        public MainPage()
        {
            this.InitializeComponent();
        }

        ~MainPage()
        {
            _ = hubConnection.StopAsync();
        }

        /// <summary>
        /// 通知系统属性已经发生更改
        /// </summary>
        /// <param name="propertyName">发生更改的属性名称,其填充是自动完成的</param>
        public void OnPropertiesChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //public bool IsTimerEnabled
        //{
        //    get => _isTimerEnabled;
        //    set
        //    {
        //        _isTimerEnabled = value;
        //        if (value)
        //        {
        //            timer.Start();
        //        }
        //        else
        //        {
        //            timer.Stop();
        //        }
        //        OnPropertiesChanged();
        //    }
        //}

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

        public Visibility ServiceInfoControlVisibility
        {
            get => _serviceInfoControlVisibility;
            set
            {
                _serviceInfoControlVisibility = value;
                OnPropertiesChanged();
            }
        }

        private async void ConnectServer(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(ServiceUri) || !Uri.TryCreate(ServiceUri, UriKind.Absolute, out _))
            {
                ShowInfoBar("无效的服务地址", "请检查你输入的值。", InfoBarSeverity.Warning);
                return;
            }

            IsServiceControlEnabled = false;
            if (IsInfoBarOpen)
            {
                IsInfoBarOpen = false;
            }

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
                ServiceInfoControlVisibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowInfoBar("无法与服务器建立联系", $"请检查服务是否打开,以及是否可以连接到Internet。\n详细信息:\n{ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                IsServiceControlEnabled = true;
            }
        }

        private async Task OnHubReconnected(string arg)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                IsServiceControlEnabled = true;
                ShowInfoBar("已重新连接", null, InfoBarSeverity.Success);
            });
        }

        private async Task OnHubReconnecting(Exception arg)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (IsInfoBarOpen)
                {
                    IsInfoBarOpen = false;
                }
                IsServiceControlEnabled = false;
                if (arg is null)
                {
                    ShowInfoBar("正在重新连接...", null, InfoBarSeverity.Warning);
                }
                else
                {
                    ShowInfoBar("正在重新连接...", $"由于名为{arg.GetType().FullName}的错误,连接被迫断开", InfoBarSeverity.Warning);
                }
            });
        }

        private async Task OnHubClosed(Exception arg)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (IsInfoBarOpen)
                {
                    IsInfoBarOpen = false;
                }
                IsServiceControlEnabled = true;
                ServiceInfoControlVisibility = Visibility.Collapsed;
                if (arg is null)
                {
                    ShowInfoBar("已断开连接", null, InfoBarSeverity.Warning);
                }
                else
                {
                    ShowInfoBar("已断开连接", $"由于名为{arg.GetType().FullName}的错误,连接被迫断开", InfoBarSeverity.Error);
                }
            });
        }

        private async void UpdateInfo(object sender, RoutedEventArgs e)
        {
            try
            {
                IsServiceControlEnabled = false;
                if (IsInfoBarOpen)
                {
                    IsInfoBarOpen = false;
                }

                if (hubConnection.State == HubConnectionState.Disconnected)
                {
                    ShowInfoBar("已断开连接", "与服务器的连接已经丢失", InfoBarSeverity.Warning);
                    IsServiceControlEnabled = true;
                    ServiceInfoControlVisibility = Visibility.Collapsed;
                    return;
                }

                IsCombustibleGasDetected = await hubConnection.InvokeAsync<bool>("GetCombustibleGasInfo");
                string envInfoString = await hubConnection.InvokeAsync<string>("GetEnvironmentInfo");
                EnvironmentInfo envInfo = JsonSerializer.Deserialize<EnvironmentInfo>(envInfoString);
                Temperature = envInfo.Temperature;
                RelativeHumidity = envInfo.RelativeHumidity;
                ServiceInfoControlVisibility = Visibility.Visible;
                if (IsCombustibleGasDetected)
                {
                    ShowGasWarning();
                }
                ShowTile();
            }
            catch (HttpRequestException ex)
            {
                ShowInfoBar("无法与服务器建立联系", $"请检查服务是否打开,以及是否可以连接到Internet。\n详细信息:\n{ex.Message}", InfoBarSeverity.Error);
            }
            catch (HubException ex)
            {
                ShowInfoBar("服务器端出现问题", $"请稍等片刻,然后重试。\n详细信息:\n{ex.Message}", InfoBarSeverity.Error);
            }
            catch (Exception ex)
            {
                ShowInfoBar("未知错误", $"详细信息:\n{ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                LastCheckTime = DateTimeOffset.Now;
                IsServiceControlEnabled = true;
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
            _ = await Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:stevemc123456@outlook.com"));
        }

        private async void GoToGithub(object sender, RoutedEventArgs e)
        {
            _ = await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/Baka632/Live-Home"));
        }

        private void ShowTile()
        {
            string combustibleGasConditon;
            if (IsCombustibleGasDetected)
            {
                combustibleGasConditon = "检测到可燃气体!";
            }
            else
            {
                combustibleGasConditon = "未发现可燃气体";
            }
            var tileContent = new TileContent()
            {
                Visual = new TileVisual()
                {
                    Branding = TileBranding.Name,
                    TileSmall = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            TextStacking = TileTextStacking.Center,
                            Children =
                {
                    new AdaptiveText()
                    {
                        Text = $"{Temperature}℃",
                        HintAlign = AdaptiveTextAlign.Center
                    },
                    new AdaptiveText()
                    {
                        Text = $"{RelativeHumidity}%",
                        HintAlign = AdaptiveTextAlign.Center
                    }
                }
                        }
                    },
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                {
                    new AdaptiveText()
                    {
                        Text = $"{ServiceUri}的环境情况",
                        HintStyle = AdaptiveTextStyle.Caption
                    },
                    new AdaptiveText()
                    {
                        Text = $"温度:{Temperature}℃",
                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                    },
                    new AdaptiveText()
                    {
                        Text = $"湿度:{RelativeHumidity}%",
                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                    }
                }
                        }
                    },
                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                {
                    new AdaptiveGroup()
                    {
                        Children =
                        {
                            new AdaptiveSubgroup()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = $"{ServiceUri}的环境情况",
                                        HintStyle = AdaptiveTextStyle.Caption
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = $"温度:{Temperature}℃",
                                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = $"湿度:{RelativeHumidity}%",
                                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = combustibleGasConditon,
                                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                                    }
                                }
                            }
                        }
                    }
                }
                        }
                    },
                    TileLarge = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                {
                    new AdaptiveGroup()
                    {
                        Children =
                        {
                            new AdaptiveSubgroup()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = $"{ServiceUri}的环境情况",
                                        HintStyle = AdaptiveTextStyle.Caption
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = $"温度:{Temperature}℃",
                                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = $"湿度:{RelativeHumidity}%",
                                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = combustibleGasConditon,
                                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                                    },
                                    new AdaptiveText()
                                    {
                                        Text = $"最后检查时间:{DateTimeOffset.Now:M}{DateTimeOffset.Now:t}",
                                        HintStyle = AdaptiveTextStyle.CaptionSubtle
                                    }
                                }
                            }
                        }
                    }
                }
                        }
                    }
                }
            };

            // Create the tile notification
            var tileNotif = new TileNotification(tileContent.GetXml());

            // And send the notification to the primary tile
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
        }

        public void ReceiveEnvironmentInfo(string message)
        {
            EnvironmentInfo envInfo = JsonSerializer.Deserialize<EnvironmentInfo>(message);
            Temperature = envInfo.Temperature;
            RelativeHumidity = envInfo.RelativeHumidity;
            LastCheckTime = DateTimeOffset.Now;
            ShowTile();
        }

        public void ReceiveCombustibleGasInfo(bool message)
        {
            IsCombustibleGasDetected = message;
            if (IsCombustibleGasDetected)
            {
                ShowGasWarning();
            }
            LastCheckTime = DateTimeOffset.Now;
            ShowTile();
        }

        private async void DisconnectServer(object sender, RoutedEventArgs e)
        {
            await hubConnection.StopAsync();
        }
    }
}
