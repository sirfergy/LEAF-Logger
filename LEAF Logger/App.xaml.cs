namespace LEAFLogger
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Device.Location;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO.IsolatedStorage;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Markup;
    using System.Windows.Navigation;
    using LEAFLogger.Resources;
    using LEAFLogger.ViewModels;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Data.Linq;
    using Microsoft.Phone.Shell;
    using Models;
    using Windows.Devices.Geolocation;

    public partial class App : Application
    {
        internal const string ObdDbConnectionString = "Data Source=isostore:/ObdDb.sdf";
        private const int CurrentDatabaseVersion = 3;

        public static readonly string[] Scopes =
            new string[] 
            { 
                "wl.signin",
                "wl.offline_access",
                "wl.skydrive",
                "wl.skydrive_update" 
            };

        public static MainViewModel MainViewModel = new MainViewModel()
        {
#if DEBUG
            IsTrial = false
#else
            IsTrial = Windows.ApplicationModel.Store.CurrentApp.LicenseInformation.IsTrial
#endif
        };

        public static Geolocator Geolocator { get; set; }

        public static bool SkyDriveEnabled { get; set; }

        public static bool RunningInBackground { get; set; }

        public static bool RunningInEmulator
        {
            get
            {
                return Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator;
            }
        }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

            // Show graphics profiling information while debugging.
#if DEBUG
            if (App.RunningInEmulator)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode,
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Prevent the screen from turning off while under the debugger by disabling
                // the application's idle detection.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
#endif

            using (ModelsDataContext db = new ModelsDataContext(ObdDbConnectionString))
            {
                if (db.DatabaseExists() == false)
                {
                    // Create the local database. 
                    db.CreateDatabase();

                    DatabaseSchemaUpdater dbUpdate = db.CreateDatabaseSchemaUpdater();
                    dbUpdate.DatabaseSchemaVersion = App.CurrentDatabaseVersion;
                    dbUpdate.Execute();
                }
                else
                {
                    //Create the database schema updater
                    DatabaseSchemaUpdater dbUpdate = db.CreateDatabaseSchemaUpdater();

                    //Get database version
                    bool updateNeeded = false;
                    int dbVersion = dbUpdate.DatabaseSchemaVersion;

                    if (dbVersion == 0)
                    {
                        db.DeleteDatabase();
                        db.CreateDatabase();
                        updateNeeded = true;
                    }

                    if (dbVersion < App.CurrentDatabaseVersion && dbVersion < 2)
                    {
                        dbVersion = 2;
                        dbUpdate.AddColumn<ObdModel>("Temperature1");
                        dbUpdate.AddColumn<ObdModel>("Temperature2");
                        dbUpdate.AddColumn<ObdModel>("Temperature3");
                        dbUpdate.AddColumn<ObdModel>("Temperature4");
                        updateNeeded = true;
                    }

                    if (dbVersion < App.CurrentDatabaseVersion && dbVersion < 3)
                    {
                        dbVersion = 3;
                        dbUpdate.AddColumn<TripModel>("MinimumTemperature1");
                        dbUpdate.AddColumn<TripModel>("MinimumTemperature2");
                        dbUpdate.AddColumn<TripModel>("MinimumTemperature3");
                        dbUpdate.AddColumn<TripModel>("MinimumTemperature4");
                        dbUpdate.AddColumn<TripModel>("MaximumTemperature1");
                        dbUpdate.AddColumn<TripModel>("MaximumTemperature2");
                        dbUpdate.AddColumn<TripModel>("MaximumTemperature3");
                        dbUpdate.AddColumn<TripModel>("MaximumTemperature4");
                        dbUpdate.AddColumn<TripModel>("AverageTemperature1");
                        dbUpdate.AddColumn<TripModel>("AverageTemperature2");
                        dbUpdate.AddColumn<TripModel>("AverageTemperature3");
                        dbUpdate.AddColumn<TripModel>("AverageTemperature4");
                        updateNeeded = true;
                    }

                    if (updateNeeded)
                    {
                        dbUpdate.DatabaseSchemaVersion = App.CurrentDatabaseVersion;
                        dbUpdate.Execute();
                    }
                }
            }

            App.MainViewModel.LoadData();
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            int applicationLoadCount = 0;
            IsolatedStorageSettings.ApplicationSettings.TryGetValue<int>("applicationLoadCount", out applicationLoadCount);

            IsolatedStorageSettings.ApplicationSettings["applicationLoadCount"] = applicationLoadCount + 1;
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            RunningInBackground = false;

            // Ensure that application state is restored appropriately
            if (!e.IsApplicationInstancePreserved)
            {
                if (PhoneApplicationService.Current.State.ContainsKey("MainViewModel"))
                {
                    App.MainViewModel = (MainViewModel)PhoneApplicationService.Current.State["MainViewModel"];
                    App.MainViewModel.LoadData();
                }
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Ensure that required application state is persisted here.
            PhoneApplicationService.Current.State["MainViewModel"] = App.MainViewModel;
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        private void Application_RunningInBackground(object sender, RunningInBackgroundEventArgs args)
        {
            RunningInBackground = true;
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (App.RunningInEmulator)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (App.RunningInEmulator)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (App.RunningInEmulator)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }

    #region Visibility Converters
    public class BooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is bool)) return Visibility.Collapsed;

            bool invert = false;
            if (parameter is string)
            {
                invert = ((string)parameter) == "Invert";
            }

            if (invert)
            {
                return ((bool)value) ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CollectionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is ICollection)) return Visibility.Collapsed;

            bool invert = false;
            if (parameter is string)
            {
                invert = ((string)parameter) == "Invert";
            }

            if (invert)
            {
                return ((ICollection)value).Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return ((ICollection)value).Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EmptyGeoCoordinateVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is GeoCoordinate)) return Visibility.Collapsed;

            GeoCoordinate geo = (GeoCoordinate)value;
            bool invert = false;
            if (parameter is string)
            {
                invert = ((string)parameter) == "Invert";
            }

            if (invert)
            {
                return geo.Latitude == 0 && geo.Longitude == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return geo.Latitude == 0 && geo.Longitude == 0 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool invert = false;
            if (parameter is string)
            {
                invert = ((string)parameter) == "Invert";
            }

            if (invert)
            {
                return value == null ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return value == null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Battery Color Converter
    public class BatteryColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                double percentage = (double)value;
                if (percentage > 0 && percentage <= 0.15)
                {
                    return "Red";
                }
                else if (percentage > 0.15 && percentage <= 0.33)
                {
                    return "Yellow";
                }
                else
                {
                    return "Green";
                }
            }
            else
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    public class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _lambdaComparer;
        private readonly Func<T, int> _lambdaHash;

        public LambdaComparer(Func<T, T, bool> lambdaComparer) :
            this(lambdaComparer, o => 0)
        {
        }

        public LambdaComparer(Func<T, T, bool> lambdaComparer, Func<T, int> lambdaHash)
        {
            if (lambdaComparer == null)
                throw new ArgumentNullException("lambdaComparer");
            if (lambdaHash == null)
                throw new ArgumentNullException("lambdaHash");

            _lambdaComparer = lambdaComparer;
            _lambdaHash = lambdaHash;
        }

        public bool Equals(T x, T y)
        {
            return _lambdaComparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _lambdaHash(obj);
        }
    }
}