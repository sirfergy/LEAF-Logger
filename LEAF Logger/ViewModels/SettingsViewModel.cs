namespace LEAFLogger.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.IO.IsolatedStorage;

    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string bluetoothDeviceName;
        private bool locationConsent;
        private bool promptedForLocation;
        private bool liveTileEnabled;
        private bool autoUpload;

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public string BluetoothDeviceName
        {
            get
            {
                if (App.RunningInEmulator)
                {
                    return "OBDII";
                }
                else
                {
                    return this.bluetoothDeviceName;
                }
            }

            set
            {
                if (this.bluetoothDeviceName != value)
                {
                    this.bluetoothDeviceName = value;
                    NotifyPropertyChanged("BluetoothDeviceName");
                }
            }
        }

        public bool LocationConsent
        {
            get
            {
                if (App.RunningInEmulator)
                {
                    return true;
                }
                else
                {
                    return this.locationConsent;
                }
            }

            set
            {
                if (this.locationConsent != value)
                {
                    this.locationConsent = value;
                    NotifyPropertyChanged("LocationConsent");
                }
            }
        }

        public bool PromptedForLocation
        {
            get
            {
                return this.promptedForLocation;
            }

            set
            {
                if (this.promptedForLocation != value)
                {
                    this.promptedForLocation = value;
                    NotifyPropertyChanged("PromptedForLocation");
                }
            }
        }

        public bool LiveTileEnabled
        {
            get
            {
                if (App.RunningInEmulator)
                {
                    return true;
                }
                else
                {
                    return this.liveTileEnabled;
                }
            }

            set
            {
                if (this.liveTileEnabled != value)
                {
                    this.liveTileEnabled = value;
                    NotifyPropertyChanged("LiveTileEnabled");
                }
            }
        }

        public bool AutoUpload
        {
            get
            {
                if (App.RunningInEmulator)
                {
                    return true;
                }
                else
                {
                    return this.autoUpload;
                }
            }

            set
            {
                if (this.autoUpload != value)
                {
                    this.autoUpload = value;
                    NotifyPropertyChanged("AutoUpload");
                }
            }
        }

        public void SaveData()
        {
            IsolatedStorageSettings.ApplicationSettings["BluetoothDeviceName"] = this.bluetoothDeviceName;
            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = this.locationConsent;
            IsolatedStorageSettings.ApplicationSettings["PromptedForLocation"] = this.promptedForLocation;
            IsolatedStorageSettings.ApplicationSettings["LiveTileEnabled"] = this.liveTileEnabled;
            IsolatedStorageSettings.ApplicationSettings["AutoUpload"] = this.autoUpload;

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public void LoadData()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.TryGetValue<string>("BluetoothDeviceName", out this.bluetoothDeviceName))
            {
                this.bluetoothDeviceName = "OBDII";
            }

            IsolatedStorageSettings.ApplicationSettings.TryGetValue<bool>("LocationConsent", out this.locationConsent);
            IsolatedStorageSettings.ApplicationSettings.TryGetValue<bool>("PromptedForLocation", out this.promptedForLocation);
            IsolatedStorageSettings.ApplicationSettings.TryGetValue<bool>("LiveTileEnabled", out this.liveTileEnabled);
            IsolatedStorageSettings.ApplicationSettings.TryGetValue<bool>("AutoUpload", out this.autoUpload);

            this.IsDataLoaded = true;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
