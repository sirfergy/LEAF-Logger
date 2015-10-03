namespace LEAFLogger
{
    using System;
    using System.Linq;
    using System.Windows;
    using Microsoft.Live;
    using Microsoft.Live.Controls;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;
    using Microsoft.Phone.Tasks;
    using ObdServices;
    using Windows.Networking.Proximity;

    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();

            this.Loaded += Settings_Loaded;
        }

        private async void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
                var peers = await PeerFinder.FindAllPeersAsync();
                var displayNames = peers.Where(x => !string.IsNullOrEmpty(x.DisplayName)).Select(x => x.DisplayName).ToList();

                devices.ItemsSource = displayNames;

                // Need to handle the case when the device no longer is paired
                if (!displayNames.Contains(App.MainViewModel.SettingsViewModel.BluetoothDeviceName))
                {
                    App.MainViewModel.SettingsViewModel.BluetoothDeviceName = null;
                }
            }
            catch (Exception ex)
            {
                // Unable to connect to bluetooth, so just clear the value.
                App.MainViewModel.SettingsViewModel.BluetoothDeviceName = null;

                if ((uint)ex.HResult == ObdErrorCodes.ERR_BLUETOOTH_OFF)
                {
                    var result = MessageBox.Show("Bluetooth is turned off, would you like to see the current Bluetooth settings?", "Bluetooth Off", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        ConnectionSettingsTask connectionSettingsTask = new ConnectionSettingsTask();
                        connectionSettingsTask.ConnectionSettingsType = ConnectionSettingsType.Bluetooth;
                        connectionSettingsTask.Show();
                    }
                }
                else
                {
                    MessageBox.Show("Unable to query for Bluetooth devices.\n" + ex.Message, "Error", MessageBoxButton.OK);
                }
            }
            finally
            {
                if (!App.RunningInEmulator)
                {
                    this.DataContext = App.MainViewModel;
                }
            }
        }

        private async void LockSettings_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();
            emailComposeTask.To = "EMAIL";
            emailComposeTask.Subject = string.Format("{0} Support", App.MainViewModel.ApplicationName);

            emailComposeTask.Show();
        }

        private void ReviewButton_Click(object sender, RoutedEventArgs e)
        {
            MarketplaceReviewTask reviewTask = new MarketplaceReviewTask();
            reviewTask.Show();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!App.MainViewModel.SettingsViewModel.LiveTileEnabled)
            {
                ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault();
                if (shellTile != null)
                {
                    shellTile.Update(
                        new FlipTileData()
                        {
                            BackContent = string.Empty,
                            BackBackgroundImage = new Uri(string.Empty, UriKind.Relative),
                            WideBackContent = string.Empty,
                            WideBackBackgroundImage = new Uri(string.Empty, UriKind.Relative),
                            Count = 0
                        });
                }
            }

            App.MainViewModel.SettingsViewModel.SaveData();
        }

        private void Signin_SessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                App.SkyDriveEnabled = true;
                this.autoUpload.IsEnabled = true;
            }
        }
    }
}