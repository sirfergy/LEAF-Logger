namespace LEAFLogger
{
    using System;
    using System.Collections.ObjectModel;
    using System.Device.Location;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using CsvHelper;
    using ImageTools;
    using ImageTools.IO;
    using ImageTools.IO.Png;
    using LEAFLogger.UserControls;
    using Microsoft.Live;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Maps;
    using Microsoft.Phone.Maps.Controls;
    using Microsoft.Phone.Maps.Toolkit;
    using Microsoft.Phone.Shell;
    using Microsoft.Phone.Tasks;
    using Models;
    using ObdServices;
    using Telerik.Charting;
    using Telerik.Windows.Controls;
    using Windows.Devices.Geolocation;

    public partial class MainPage : PhoneApplicationPage
    {
        private const string BackBackgroundImage = "/Shared/ShellContent/BackBackgroundImage.png";
        private const string WideBackBackgroundImage = "/Shared/ShellContent/WideBackBackgroundImage.png";

        private PeerInformationWrapper device = null;
        private IObdBluetoothService service = GetObdBluetoothService();
        private bool tracking = false;
        private bool progressIndicatorVisibility = false;
        private MapPolyline route;
        private Timer serviceUpdateTimer;
        private GeoCoordinate lastGeoCoordinate;
        private PngEncoder encoder = new PngEncoder();
        private UserControl wideBackTileUserControl = new WideBackTileUserControl();
        private UserControl backTileUserControl = new BackTileUserControl();

        // debug only
        private static Random random = new Random();

        #region Charts
        #region SoC Chart
        private DateTimeContinuousAxis socHorizontalAxis = new DateTimeContinuousAxis()
        {
            LabelFitMode = AxisLabelFitMode.Rotate,
            LabelFormat = "h:mm:ss",
            LabelInterval = 5
        };

        private LinearAxis socVerticalAxis = new LinearAxis()
        {
            Maximum = 1,
            LabelFormat = "P0"
        };

        private CartesianSeries socSeries = new AreaSeries()
        {
            ItemsSource = App.MainViewModel.MapObdUpdates,
            Stroke = (Brush)App.Current.Resources["PhoneAccentBrush"],
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "DisplayWhen"
            },
            ValueBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "SoC"
            }
        };
        #endregion

        #region Elevation Chart
        private DateTimeContinuousAxis elevationHorizontalAxis = new DateTimeContinuousAxis()
        {
            LabelFitMode = AxisLabelFitMode.Rotate,
            LabelFormat = "h:mm:ss",
            LabelInterval = 25
        };

        private LinearAxis elevationVerticalAxis = new LinearAxis()
        {
            LabelFormat = "0 ft"
        };

        private CartesianSeries elevationSeries = new SplineSeries()
        {
            ItemsSource = App.MainViewModel.Geocoordinates,
            Stroke = (Brush)App.Current.Resources["PhoneAccentBrush"],
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "DisplayTimestamp"
            },
            ValueBinding = new GenericDataPointBinding<GeocoordinateModel, double>()
            {
                ValueSelector = (x => App.RunningInEmulator ? random.Next(-1000, 1000) : x.GeoCoordinate.Altitude * 3.2808399)
            }
        };
        #endregion

        #region Speed Chart
        private DateTimeContinuousAxis speedHorizontalAxis = new DateTimeContinuousAxis()
        {
            LabelFitMode = AxisLabelFitMode.Rotate,
            LabelFormat = "h:mm:ss",
            LabelInterval = 25
        };

        private LinearAxis speedVerticalAxis = new LinearAxis()
        {
            Minimum = 0,
            Maximum = 94,
            LabelFormat = "0 mph"
        };

        private CartesianSeries speedSeries = new SplineSeries()
        {
            ItemsSource = App.MainViewModel.Geocoordinates,
            Stroke = (Brush)App.Current.Resources["PhoneAccentBrush"],
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "DisplayTimestamp"
            },
            ValueBinding = new GenericDataPointBinding<GeocoordinateModel, double>()
            {
                ValueSelector = (x => App.RunningInEmulator ? random.Next(0, 94) : x.GeoCoordinate.Speed * 2.23693629)
            }
        };
        #endregion

        #region Distance Chart
        private CategoricalAxis distanceHorizontalAxis = new CategoricalAxis()
        {
            LabelFitMode = AxisLabelFitMode.Rotate,
            LabelInterval = 4,
        };

        private LinearAxis distanceVerticalAxis = new LinearAxis()
        {
            Maximum = 1,
            LabelFormat = "P0"
        };

        private CartesianSeries distanceSeries = new AreaSeries()
        {
            ItemsSource = App.MainViewModel.MapObdUpdates,
            Stroke = (Brush)App.Current.Resources["PhoneAccentBrush"],
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "TotalDistanceRounded"
            },
            ValueBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "SoC"
            }
        };
        #endregion

        #region Efficiency Chart
        private CategoricalAxis efficiencyHorizontalAxis = new CategoricalAxis()
        {
            LabelFitMode = AxisLabelFitMode.Rotate,
            LabelInterval = 4,
        };

        private LinearAxis efficiencyVerticalAxis = new LinearAxis()
        {
            LabelFormat = "N2"
        };

        private CartesianSeries efficiencySeries = new AreaSeries()
        {
            ItemsSource = App.MainViewModel.MapObdUpdates,
            Stroke = (Brush)App.Current.Resources["PhoneAccentBrush"],
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "TotalDistanceRounded"
            },
            ValueBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "AverageEnergyEconomy"
            }
        };
        #endregion

        #region Temperature Chart
        private DateTimeContinuousAxis temperatureHorizontalAxis = new DateTimeContinuousAxis()
        {
            LabelFitMode = AxisLabelFitMode.Rotate,
            LabelFormat = "h:mm:ss",
            LabelInterval = 5
        };

        private LinearAxis temperatureVerticalAxis = new LinearAxis()
        {
            LabelFormat = "0°",
            Minimum = 30,
            Maximum = 100
        };

        private CartesianSeries temperature1Series = new LineSeries()
        {
            ItemsSource = App.MainViewModel.MapObdUpdates,
            Stroke = new SolidColorBrush(Colors.Green),
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "DisplayWhen"
            },
            ValueBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "Temperature1"
            }
        };

        private CartesianSeries temperature2Series = new LineSeries()
        {
            ItemsSource = App.MainViewModel.MapObdUpdates,
            Stroke = new SolidColorBrush(Colors.Blue),
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "DisplayWhen"
            },
            ValueBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "Temperature2"
            }
        };

        private CartesianSeries temperature3Series = new LineSeries()
        {
            ItemsSource = App.MainViewModel.MapObdUpdates,
            Stroke = new SolidColorBrush(Colors.Yellow),
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "DisplayWhen"
            },
            ValueBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "Temperature3"
            }
        };

        private CartesianSeries temperature4Series = new LineSeries()
        {
            ItemsSource = App.MainViewModel.MapObdUpdates,
            Stroke = new SolidColorBrush(Colors.Red),
            StrokeThickness = 2,
            CategoryBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "DisplayWhen"
            },
            ValueBinding = new PropertyNameDataPointBinding()
            {
                PropertyName = "Temperature4"
            }
        };
        #endregion
        #endregion

        public MainPage()
        {
            InitializeComponent();

            Encoders.AddEncoder<PngEncoder>();

            this.DataContext = App.MainViewModel;

            this.Loaded += MainPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DisplayApplicationBar();
            this.SetProgressIndicatorVisibility(this.progressIndicatorVisibility);
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainPage_Loaded;

            if (App.MainViewModel.IsTrial)
            {
                this.pivot.Items.Remove(this.track);
                this.pivot.Items.Remove(this.charts);
                this.pivot.Items.Remove(this.trips);
                this.capacityLabel.Visibility = Visibility.Collapsed;
                this.capacityControl.Visibility = Visibility.Collapsed;
            }

            route = new MapPolyline();
            route.StrokeColor = Colors.Red;
            route.StrokeThickness = 5;
            route.Path = App.MainViewModel.MapPath;

            this.MapExtensionsSetup(this.map);
            this.map.MapElements.Add(this.route);

            if (App.MainViewModel.Geocoordinates.Count > 0)
            {
                this.lastGeoCoordinate = App.MainViewModel.Geocoordinates.Last().GeoCoordinate;
                this.map.Center = this.lastGeoCoordinate;

                if (App.MainViewModel.MapObdUpdates.Count > 0)
                {
                    this.dataPivotItem.DataContext = App.MainViewModel.MapObdUpdates.Last();
                    this.track.DataContext = App.MainViewModel.MapObdUpdates.Last();
                }
            }
            else
            {
                this.GetObdData();
            }

            App.SkyDriveEnabled = await this.InitializeSkyDrive() != null;

            if (App.MainViewModel.ApplicationLoadCount >= 5 &&
                !App.MainViewModel.PromptedForReview)
            {
                if (MessageBox.Show("Would you like to review LEAF Logger?", "Review?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    MarketplaceReviewTask reviewTask = new MarketplaceReviewTask();
                    reviewTask.Show();
                }
            }

            this.FeedbackOverlay.VisibilityChanged += (object s, EventArgs ea) =>
            {
                ApplicationBar.IsVisible = (FeedbackOverlay.Visibility != Visibility.Visible);
            };
        }

        private async Task<LiveConnectSession> InitializeSkyDrive()
        {
            try
            {
                var liveAuthClient = new LiveAuthClient("CLIENTID");
                var liveLoginResult = await liveAuthClient.InitializeAsync();
                if (liveLoginResult.Status == LiveConnectSessionStatus.Connected)
                {
                    return liveLoginResult.Session;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Unable to connect to SkyDrive\n" + ex.Message, "Error", MessageBoxButton.OK);
                });
            }

            return null;
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            this.GetObdData();
        }

        private void GetObdData()
        {
            if (App.RunningInEmulator &&
                this.progressIndicatorVisibility)
            {
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                this.SetProgressIndicatorVisibility(true);
            });

            ThreadPool.QueueUserWorkItem(async (object state) =>
            {
                // If user changes the device name, null it out so that future calls will re-create the device.
                if (this.device != null &&
                    this.device.DisplayName != App.MainViewModel.SettingsViewModel.BluetoothDeviceName)
                {
                    this.device = null;
                }

                if (this.device == null)
                {
                    try
                    {
                        this.device = await this.service.GetObdDevice(App.MainViewModel.SettingsViewModel.BluetoothDeviceName);
                        if (this.device == null &&
                            this.RunningInForeground())
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                MessageBox.Show("No OBD II devices found of name " + App.MainViewModel.SettingsViewModel.BluetoothDeviceName, "Error", MessageBoxButton.OK);
                                this.SetProgressIndicatorVisibility(false);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            this.SetProgressIndicatorVisibility(false);
                            if (this.RunningInForeground())
                            {
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
                                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                                }
                            }
                        });
                    }
                }

                if (this.device != null)
                {
                    try
                    {
                        ObdModel data = await this.service.GetObdData(this.device);
                        this.DisplayObdData(data);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            this.SetProgressIndicatorVisibility(false);
                            if (this.RunningInForeground())
                            {
                                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                            }
                        });
                    }
                }
            });
        }

        private void DisplayObdData(ObdModel obdModel)
        {
            obdModel.GeoCoordinate = this.lastGeoCoordinate;
            TimeSpan duration;

            if (this.tracking &&
                App.MainViewModel.MapObdUpdates.Count > 0 &&
                App.MainViewModel.Geocoordinates.Count() > 1 &&
                this.lastGeoCoordinate != null)
            {
                // Get the first map update
                ObdModel firstMapUpdate = App.MainViewModel.MapObdUpdates.First();

                // get the last map obd update
                ObdModel lastMapUpdate = App.MainViewModel.MapObdUpdates.Last();

                // trip duration
                duration = lastMapUpdate.When.Subtract(firstMapUpdate.When).Duration();

                // get all points since last check
                var geoCoordinates = App.MainViewModel.Geocoordinates.Where(x => x.Timestamp > lastMapUpdate.When).Select(x => x.GeoCoordinate).ToList();

                // add last map update to the beginning of the list
                geoCoordinates.Insert(0, lastMapUpdate.GeoCoordinate);

                // calculate the distance from the last map update to now
                double distance = 0;
                double ascent = 0;
                double descent = 0;
                GeoCoordinate first, second;

                for (int i = 0; i < geoCoordinates.Count - 1; i++)
                {
                    first = geoCoordinates[i];
                    second = geoCoordinates[i + 1];

                    distance += first.GetDistanceTo(second);

                    if (second.Altitude > first.Altitude)
                    {
                        ascent += (second.Altitude - first.Altitude);
                    }
                    else if (first.Altitude > second.Altitude)
                    {
                        descent += (first.Altitude - second.Altitude);
                    }
                }

                obdModel.Distance = distance / 1609.344;
                obdModel.TotalDistance = lastMapUpdate.TotalDistance + obdModel.Distance;

                obdModel.Ascent = ascent;
                obdModel.TotalAscent = lastMapUpdate.TotalAscent + ascent;

                obdModel.Descent = descent;
                obdModel.TotalDescent = lastMapUpdate.TotalDescent + descent;

                obdModel.KiloWattHoursUsed = lastMapUpdate.AvailableKiloWattHours - obdModel.AvailableKiloWattHours;
                obdModel.TotalKiloWattHoursUsed = firstMapUpdate.AvailableKiloWattHours - obdModel.AvailableKiloWattHours;
            }

            Dispatcher.BeginInvoke(() =>
            {
                this.SetProgressIndicatorVisibility(false);

                this.dataPivotItem.DataContext = obdModel;
                this.track.DataContext = obdModel;

                if (this.tracking &&
                    this.lastGeoCoordinate != null)
                {
                    if (App.MainViewModel.SettingsViewModel.LiveTileEnabled)
                    {
                        ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault();
                        if (shellTile != null)
                        {
                            wideBackTileUserControl.DataContext = obdModel;
                            backTileUserControl.DataContext = obdModel;
                            TextBlock backTileDuration = (TextBlock)backTileUserControl.FindName("duration");
                            TextBlock wideBackTileDuration = (TextBlock)wideBackTileUserControl.FindName("duration");

                            backTileDuration.Text = string.Format("Trip Duration: {0:h\\:mm\\:ss}", duration);
                            wideBackTileDuration.Text = string.Format("Trip Duration: {0:h\\:mm\\:ss}", duration);

                            ExtendedImage wideBackTileImage = wideBackTileUserControl.ToImage();
                            ExtendedImage backTileImage = backTileUserControl.ToImage();

                            using (var storageFile = IsolatedStorageFile.GetUserStoreForApplication())
                            {
                                if (storageFile.FileExists(BackBackgroundImage))
                                {
                                    storageFile.DeleteFile(BackBackgroundImage);
                                }

                                if (storageFile.FileExists(WideBackBackgroundImage))
                                {
                                    storageFile.DeleteFile(WideBackBackgroundImage);
                                }

                                using (var stream = new IsolatedStorageFileStream(WideBackBackgroundImage, FileMode.Create, FileAccess.Write, storageFile))
                                {
                                    encoder.Encode(wideBackTileImage, stream);
                                }

                                using (var stream = new IsolatedStorageFileStream(BackBackgroundImage, FileMode.Create, FileAccess.Write, storageFile))
                                {
                                    encoder.Encode(backTileImage, stream);
                                }
                            }

                            shellTile.Update(
                                new FlipTileData()
                                {
                                    BackContent = string.Empty,
                                    WideBackContent = string.Empty,
                                    BackBackgroundImage = new Uri("isostore:" + BackBackgroundImage, UriKind.Absolute),
                                    WideBackBackgroundImage = new Uri("isostore:" + WideBackBackgroundImage, UriKind.Absolute),
                                    Count = (int)(obdModel.SoC * 100.0)
                                });
                        }
                    }

                    App.MainViewModel.Add(obdModel);
                }
            });
        }

        /// <summary>
        /// Setup the map extensions objects.
        /// All named objects inside the map extensions will have its references properly set
        /// </summary>
        /// <param name="map">The map that uses the map extensions</param>
        private void MapExtensionsSetup(Map map)
        {
            MapsSettings.ApplicationContext.ApplicationId = "59ce70ce-9604-4bec-8330-79bcce797eed";
            MapsSettings.ApplicationContext.AuthenticationToken = "s8KBiJZJfTtEIg625x87wA";

            ObservableCollection<DependencyObject> children = MapExtensions.GetChildren(map);
            var runtimeFields = this.GetType().GetRuntimeFields();

            foreach (DependencyObject i in children)
            {
                var info = i.GetType().GetProperty("Name");
                if (info != null)
                {
                    string name = (string)info.GetValue(i);
                    if (name != null)
                    {
                        foreach (FieldInfo j in runtimeFields)
                        {
                            if (j.Name == name)
                            {
                                j.SetValue(this, i);
                                break;
                            }
                        }
                    }
                }
            }

            this.obdUpdatesMapItemsControl.ItemsSource = App.MainViewModel.MapObdUpdates;
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            RadInputPrompt.Show(
                    title: "Trip Name",
                    buttons: MessageBoxButtons.OK,
                    checkBoxContent: App.SkyDriveEnabled ? "Export to SkyDrive?" : null,
                    isCheckBoxChecked: App.SkyDriveEnabled ? App.MainViewModel.SettingsViewModel.AutoUpload : false,
                    closedHandler: async (args) =>
                    {
                        if (args.Result == DialogResult.OK)
                        {
                            string tripName = null;
                            string fileName = null;
                            if (string.IsNullOrEmpty(args.Text))
                            {
                                tripName = "unnamed trip";
                                fileName = string.Format("leaflogger-export-{0:yyyyMMdd-HHmmss}.csv", DateTime.Now);
                            }
                            else
                            {
                                tripName = args.Text;
                                fileName = tripName + ".csv";
                            }

                            if (args.IsCheckBoxChecked)
                            {
                                bool exportSuccess = await this.ExportToSkyDrive(fileName);
                                if (!exportSuccess)
                                {
                                    return;
                                }
                            }

                            // before reset i want to store the trip
                            TripModel trip = new TripModel(App.MainViewModel.MapObdUpdates.First(), App.MainViewModel.MapObdUpdates.Last())
                            {
                                Name = tripName,
                                MinimumTemperature1 = App.MainViewModel.MapObdUpdates.Where(x => x.Temperature1.HasValue).Min(x => x.Temperature1),
                                MinimumTemperature2 = App.MainViewModel.MapObdUpdates.Where(x => x.Temperature2.HasValue).Min(x => x.Temperature2),
                                MinimumTemperature3 = App.MainViewModel.MapObdUpdates.Where(x => x.Temperature3.HasValue).Min(x => x.Temperature3),
                                MinimumTemperature4 = App.MainViewModel.MapObdUpdates.Where(x => x.Temperature4.HasValue).Min(x => x.Temperature4),
                                MaximumTemperature1 = App.MainViewModel.MapObdUpdates.Max(x => x.Temperature1),
                                MaximumTemperature2 = App.MainViewModel.MapObdUpdates.Max(x => x.Temperature2),
                                MaximumTemperature3 = App.MainViewModel.MapObdUpdates.Max(x => x.Temperature3),
                                MaximumTemperature4 = App.MainViewModel.MapObdUpdates.Max(x => x.Temperature4),
                                AverageTemperature1 = App.MainViewModel.MapObdUpdates.Where(x => x.Temperature1.HasValue).Average(x => x.Temperature1),
                                AverageTemperature2 = App.MainViewModel.MapObdUpdates.Where(x => x.Temperature2.HasValue).Average(x => x.Temperature2),
                                AverageTemperature3 = App.MainViewModel.MapObdUpdates.Where(x => x.Temperature3.HasValue).Average(x => x.Temperature3),
                                AverageTemperature4 = App.MainViewModel.MapObdUpdates.Where(x => x.Temperature4.HasValue).Average(x => x.Temperature4)
                            };

                            App.MainViewModel.Add(trip);

                            // clear all the database entries
                            App.MainViewModel.Clear();

                            // reset the cached values
                            App.MainViewModel.MapObdUpdates.Clear();
                            App.MainViewModel.MapPath.Clear();

                            // reset the route
                            this.map.MapElements.Remove(this.route);
                            this.map.MapElements.Add(this.route);

                            this.lastGeoCoordinate = null;
                            this.track.DataContext = null;

                            Grid.SetRowSpan(this.map, 3);

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

                            this.PopulateTrackingAppButtons();
                        }
                    });
        }

        private void Start_Click(object sender, EventArgs e)
        {
            this.tracking = true;
            Grid.SetRowSpan(this.map, 1);

            if (App.Geolocator == null)
            {
                App.Geolocator = new Geolocator();
                App.Geolocator.DesiredAccuracy = PositionAccuracy.High;
                App.Geolocator.MovementThreshold = 5;
                App.Geolocator.PositionChanged += Geolocator_PositionChanged;
                App.Geolocator.StatusChanged += Geolocator_StatusChanged;
            }

            this.PopulateTrackingAppButtons();
        }

        private void Pause_Click(object sender, EventArgs e)
        {
            lock (random)
            {
                this.tracking = false;
                this.lastGeoCoordinate = null;
                if (this.serviceUpdateTimer != null)
                {
                    this.serviceUpdateTimer.Dispose();
                }

                if (App.Geolocator != null)
                {
                    App.Geolocator.PositionChanged -= Geolocator_PositionChanged;
                    App.Geolocator.StatusChanged -= Geolocator_StatusChanged;
                    App.Geolocator = null;
                }
            }

            this.userLocationMarker.Visibility = Visibility.Collapsed;

            this.PopulateTrackingAppButtons();
        }

        private void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            switch (args.Status)
            {
                case PositionStatus.Ready:
                    lock (random)
                    {
                        if (this.tracking)
                        {
                            this.serviceUpdateTimer = new Timer(
                                (object state) =>
                                {
                                    this.GetObdData();
                                },
                                null,
                                TimeSpan.FromMilliseconds(10),
                                App.RunningInEmulator ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(1));
                        }
                    }
                    break;
            }
        }

        private void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            GeocoordinateModel gvm = new GeocoordinateModel(args.Position.Coordinate);
            this.lastGeoCoordinate = gvm.GeoCoordinate;

            Dispatcher.BeginInvoke(() =>
            {
                App.MainViewModel.Add(gvm);

                this.map.Center = this.lastGeoCoordinate;

                this.userLocationMarker.GeoCoordinate = this.lastGeoCoordinate;
                this.userLocationMarker.Visibility = Visibility.Visible;
            });
        }

        private void Export_Click(object sender, EventArgs e)
        {
            if (App.SkyDriveEnabled)
            {
                RadInputPrompt.Show(
                    title: "SkyDrive Upload File Name",
                    message: "Leave empty for default file name\nFile extension \".csv\" will automatically be added",
                    buttons: MessageBoxButtons.OK,
                    closedHandler: (args) =>
                    {
                        if (args.Result == DialogResult.OK)
                        {
                            if (string.IsNullOrEmpty(args.Text))
                            {
                                this.ExportToSkyDrive(string.Format("leaflogger-export-{0:yyyyMMdd-HHmmss}.csv", DateTime.Now));
                            }
                            else
                            {
                                if (args.Text.EndsWith(".csv"))
                                {
                                    this.ExportToSkyDrive(args.Text);
                                }
                                else
                                {
                                    this.ExportToSkyDrive(args.Text + ".csv");
                                }
                            }
                        }
                    });
            }
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.pivot.SelectedIndex == 1 &&
                !this.LocationEnabled() &&
                !App.MainViewModel.SettingsViewModel.PromptedForLocation)
            {
                App.MainViewModel.SettingsViewModel.PromptedForLocation = true;
                MessageBoxResult result = MessageBox.Show("Would you like to enable location services?", "Location Services", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    this.NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
                }
            }

            this.DisplayApplicationBar();
        }

        private void DisplayApplicationBar()
        {
            switch (this.pivot.SelectedIndex)
            {
                case 0:
                    ApplicationBar.Buttons.Clear();
                    ApplicationBar.Mode = ApplicationBarMode.Default;
                    ApplicationBar.Buttons.Add((ApplicationBarIconButton)this.Resources["refresh"]);
                    break;
                case 1:
                    this.PopulateTrackingAppButtons();
                    break;
                case 2:
                    ApplicationBar.Buttons.Clear();
                    ApplicationBar.Mode = ApplicationBarMode.Minimized;
                    break;
                case 3:
                    ApplicationBar.Buttons.Clear();
                    ApplicationBar.Mode = ApplicationBarMode.Minimized;

                    break;
            }
        }

        private void PopulateTrackingAppButtons()
        {
            ApplicationBar.Buttons.Clear();

            if (this.LocationEnabled())
            {
                ApplicationBar.Mode = ApplicationBarMode.Default;

                if (this.tracking)
                {
                    ApplicationBar.Buttons.Add((ApplicationBarIconButton)this.Resources["pause"]);
                }
                else
                {
                    ApplicationBar.Buttons.Add((ApplicationBarIconButton)this.Resources["start"]);

                    ApplicationBarIconButton reset = (ApplicationBarIconButton)this.Resources["reset"];
                    reset.IsEnabled = App.MainViewModel.MapObdUpdates.Count > 0;
                    ApplicationBar.Buttons.Add(reset);
                }

                ApplicationBarIconButton export = (ApplicationBarIconButton)this.Resources["export"];
                export.IsEnabled = App.SkyDriveEnabled && (App.MainViewModel.MapPath.Count > 0 || this.tracking);
                ApplicationBar.Buttons.Add(export);
            }
            else
            {
                ApplicationBar.Mode = ApplicationBarMode.Minimized;
            }
        }

        private bool RunningInForeground()
        {
            return !App.RunningInBackground;
        }

        private bool LocationEnabled()
        {
            return App.MainViewModel.SettingsViewModel.LocationConsent;
        }

        private void SetProgressIndicatorVisibility(bool visible)
        {
            ((ApplicationBarIconButton)this.Resources["refresh"]).IsEnabled = !visible;

            progressIndicatorVisibility = visible;
            if (SystemTray.ProgressIndicator != null)
            {
                SystemTray.ProgressIndicator.IsVisible = visible;
            }
        }

        private static IObdBluetoothService GetObdBluetoothService()
        {
            if (App.RunningInEmulator)
            {
                return new MockObdBluetoothService();
            }
            else
            {
                return new ObdBluetoothService();
            }
        }

        private void chartPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null &&
                e.AddedItems.Count > 0)
            {
                string chartType = (string)e.AddedItems[0];
                switch (chartType)
                {
                    case "State of Charge (Time)":
                        this.chart.Series.Clear();

                        this.chart.HorizontalAxis = socHorizontalAxis;
                        this.chart.VerticalAxis = socVerticalAxis;
                        this.chart.Series.Add(socSeries);
                        break;

                    case "State of Charge (Distance)":
                        this.chart.Series.Clear();

                        this.chart.HorizontalAxis = distanceHorizontalAxis;
                        this.chart.VerticalAxis = distanceVerticalAxis;
                        this.chart.Series.Add(distanceSeries);
                        break;

                    case "Elevation":
                        this.chart.Series.Clear();

                        this.chart.HorizontalAxis = elevationHorizontalAxis;
                        this.chart.VerticalAxis = elevationVerticalAxis;
                        this.chart.Series.Add(elevationSeries);
                        break;
                    case "Speed":
                        this.chart.Series.Clear();

                        this.chart.HorizontalAxis = speedHorizontalAxis;
                        this.chart.VerticalAxis = speedVerticalAxis;
                        this.chart.Series.Add(speedSeries);
                        break;

                    case "Average Efficiency":
                        this.chart.Series.Clear();

                        this.chart.HorizontalAxis = efficiencyHorizontalAxis;
                        this.chart.VerticalAxis = efficiencyVerticalAxis;
                        this.chart.Series.Add(efficiencySeries);
                        break;
                    case "Battery Temperature":
                        this.chart.Series.Clear();

                        this.chart.HorizontalAxis = temperatureHorizontalAxis;
                        this.chart.VerticalAxis = temperatureVerticalAxis;
                        this.chart.Series.Add(temperature1Series);
                        this.chart.Series.Add(temperature2Series);
                        this.chart.Series.Add(temperature3Series);
                        this.chart.Series.Add(temperature4Series);
                        break;
                }
            }
        }

        private async Task<bool> ExportToSkyDrive(string filename)
        {
            Exception exception = null;
            ApplicationBar.IsVisible = false;
            SystemTray.ProgressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator.Text = "Exporting data to SkyDrive";
            this.IsEnabled = false;
            this.Opacity = 0.25;

            try
            {
                LiveConnectSession session = await this.InitializeSkyDrive();
                LiveConnectClient client = new LiveConnectClient(session);

                using (MemoryStream stream = new MemoryStream())
                using (TextWriter writer = new StreamWriter(stream))
                using (CsvWriter csvWriter = new CsvWriter(writer))
                {
                    var entries =
                        from geocoordinate in App.MainViewModel.Geocoordinates
                        join obdUpdate in App.MainViewModel.MapObdUpdates on geocoordinate.GeoCoordinate equals obdUpdate.GeoCoordinate into all
                        from obd in all.DefaultIfEmpty()
                        select (obd == null ? new ObdModel() { GeoCoordinate = geocoordinate.GeoCoordinate, When = geocoordinate.Timestamp } : obd);

                    var csv =
                        from obd in entries
                        orderby obd.When
                        select new
                        {
                            Latitude = obd.Latitude,
                            Longitude = obd.Longitude,
                            Elevation = obd.Elevation,
                            Ascent = obd.Ascent,
                            Descent = obd.Descent,
                            Speed = obd.MilesPerHour,
                            Distance = obd.Distance,
                            SoC = obd.SoC,
                            Capacity = obd.Capacity,
                            RawCapacity = obd.RawCapacity,
                            UsableKiloWattHours = obd.UsableKiloWattHours,
                            AvailableKiloWattHours = obd.AvailableKiloWattHours,
                            KiloWattHoursUsed = obd.KiloWattHoursUsed,
                            AverageEnergyEconomy = obd.AverageEnergyEconomy,
                            Range = obd.Range,
                            Timestamp = obd.When,
                            Temperature1 = obd.Temperature1,
                            Temperature2 = obd.Temperature2,
                            Temperature3 = obd.Temperature3,
                            Temperature4 = obd.Temperature4
                        };

                    csvWriter.WriteRecords(csv);

                    writer.Flush();
                    stream.Flush();
                    stream.Position = 0;

                    LiveOperationResult result = await client.UploadAsync("me/skydrive", filename, stream, OverwriteOption.Rename);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                ApplicationBar.IsVisible = true;
                SystemTray.ProgressIndicator.IsVisible = false;
                SystemTray.ProgressIndicator.Text = string.Empty;
                this.IsEnabled = true;
                this.Opacity = 1;

                if (exception != null)
                {
                    MessageBox.Show("Unable to export to SkyDrive\n" + exception.Message, "Error", MessageBoxButton.OK);
                }
            }

            return exception == null;
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel details = (StackPanel)((Grid)sender).FindName("details");
            details.Visibility = details.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Trip_Delete_Tapped(object sender, ContextMenuItemSelectedEventArgs e)
        {
            TripModel trip = (TripModel)((RadContextMenuItem)e.VisualContainer).Tag;
            App.MainViewModel.Remove(trip);
        }
    }
}