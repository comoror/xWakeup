using Microsoft.WindowsAzure.Messaging;
using Microsoft.WindowsAzure.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.PushNotifications;
using Windows.Networking.PushNotifications;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Background;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace xWakeup
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string connectionString = "Endpoint=sb://nhs-WnsPush.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=VjQofSDw3tRoT5IDrHsnIrDpMZBFG1Qclkl7GQAjRvo=";
        string notificatiobHubPath = "nh-WnsPush";
        public MainPage()
        {
            this.InitializeComponent();

            EnableBgWorkAsync();
            InitNotificationsAsync();
        }

        private async void EnableBgWorkAsync()
        {
            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                status == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity ||
                status == BackgroundAccessStatus.AllowedSubjectToSystemPolicy ||
                status == BackgroundAccessStatus.AlwaysAllowed)
            {
                Trace.WriteLine("xWakeup: " + status.ToString() + " background access granted.");
            }
            else
            {
                Trace.WriteLine("xWakeup: Background access denied.");
            }

            bool result = await BackgroundExecutionManager.RequestAccessKindForModernStandbyAsync(
                BackgroundAccessRequestKind.AlwaysAllowed, "wakeup");
            if (result)
            {
                Trace.WriteLine("xWakeup: Modern standby access granted.");
            }
            else
            {
                Trace.WriteLine("xWakeup: Modern standby access denied.");
            }
        }

        private async void InitNotificationsAsync()
        {
            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();

            var hub = new NotificationHub(notificatiobHubPath, connectionString);

            var result = await hub.RegisterNativeAsync(channel.Uri);

            // Displays the registration ID so you know it was successful
            if (result.RegistrationId != null)
            {
                channel.PushNotificationReceived += OnPushNotificationReceived;

                Trace.WriteLine("xWakeup: Registration successful: " + result.RegistrationId);
                txtRid.Text = result.RegistrationId;
                txtExpired.Text = result.ExpiresAt.ToString();

                var dialog = new MessageDialog("Registration successful: " + result.RegistrationId);
                dialog.Commands.Add(new UICommand("OK"));
                await dialog.ShowAsync();
            }
            else
            {
                Trace.WriteLine("xWakeup: Registration fail.");

                var dialog = new MessageDialog("Registration fail.");
                dialog.Commands.Add(new UICommand("Error"));
                await dialog.ShowAsync();
            }
        }

        private void OnPushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            string txtInfo = "Notification received: " + args.NotificationType.ToString();
            switch (args.NotificationType)
            {
                case PushNotificationType.Toast:
                    //如果是提示通知，显示提示
                    Trace.WriteLine("xWakeup: a Toast was received.");
                    ToastNotificationManager.CreateToastNotifier().Show(args.ToastNotification);
                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        DateTime now = DateTime.Now;
                        txtRawMsg.Text = now.ToString() + ": a Toast was received.";
                    });

                    break;
                case PushNotificationType.Tile:
                    //如果是磁贴通知，更新应用的磁贴
                    Trace.WriteLine("xWakeup: a Tile was received.");
                    TileUpdateManager.CreateTileUpdaterForApplication().Update(args.TileNotification);
                    break;
                case PushNotificationType.Badge:
                    //如果是徽章通知，更新应用的徽章
                    Trace.WriteLine("xWakeup: a Badge was received.");
                    BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(args.BadgeNotification);
                    break;
                case PushNotificationType.Raw:
                    //如果是原始通知，获取通知的内容
                    Trace.WriteLine("xWakeup: a Raw was received.");
                    string content = args.RawNotification.Content;
                    Debug.WriteLine(content);
                    _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        DateTime now = DateTime.Now;
                        txtRawMsg.Text = now.ToString() + ": a Raw was received: " + content;
                    });

                    break;
                default:
                    // Handle unknown notification type
                    break;
            }


            //取消通知的默认处理，以避免重复
            args.Cancel = true;
        }
    }
}
