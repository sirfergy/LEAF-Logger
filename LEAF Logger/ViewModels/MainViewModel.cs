namespace LEAFLogger.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Microsoft.Phone.Maps.Controls;
    using Models;

    public class MainViewModel : INotifyPropertyChanged
    {
        private bool isTrial = false;
        private int applicationLoadCount = 0;
        private bool promptedForReview = false;
        private string applicationName;
        private string applicationVersion;
        private SettingsViewModel settingsViewModel = new SettingsViewModel();
        private ObservableCollection<TripModel> trips = new ObservableCollection<TripModel>();
        private ObservableCollection<ObdModel> mapObdUpdates = new ObservableCollection<ObdModel>();
        private ObservableCollection<GeocoordinateModel> geocoordinates = new ObservableCollection<GeocoordinateModel>();
        private GeoCoordinateCollection mapPath = new GeoCoordinateCollection();
        private string[] chartTypes = new string[] { "State of Charge (Time)", "State of Charge (Distance)", "Elevation", "Speed", "Average Efficiency", "Battery Temperature" };

        private ModelsDataContext obdDb;

        public MainViewModel()
        {
            this.settingsViewModel.LoadData();

            this.obdDb = new ModelsDataContext(App.ObdDbConnectionString);

            this.mapObdUpdates.CollectionChanged += ((object sender, NotifyCollectionChangedEventArgs e) =>
            {
                this.MapObdUpdatesPropertyChanged();
            });
        }

        public bool IsTrial
        {
            get
            {
                return this.isTrial;
            }

            set
            {
                if (value != this.isTrial)
                {
                    this.isTrial = value;
                    this.NotifyPropertyChanged("IsTrial");
                }
            }
        }

        public int ApplicationLoadCount
        {
            get
            {
                return this.applicationLoadCount;
            }

            set
            {
                if (value != this.applicationLoadCount)
                {
                    this.applicationLoadCount = value;
                    this.NotifyPropertyChanged("ApplicationLoadCount");
                }
            }
        }

        public bool PromptedForReview
        {
            get
            {
                if (!this.promptedForReview)
                {
                    this.promptedForReview = true;
                    IsolatedStorageSettings.ApplicationSettings["promptedForReview"] = this.promptedForReview;
                    IsolatedStorageSettings.ApplicationSettings.Save();

                    return false;
                }
                else
                {
                    return this.promptedForReview;
                }
            }

            set
            {
                if (value != this.promptedForReview)
                {
                    this.promptedForReview = value;
                    this.NotifyPropertyChanged("PromptedForReview");
                }
            }
        }

        public string ApplicationName
        {
            get
            {
                if (this.applicationName == null)
                {
                    this.applicationName = ((AssemblyTitleAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false).First()).Title;
                }

                return this.applicationName;
            }
        }

        public string ApplicationVersion
        {
            get
            {
                if (this.applicationVersion == null)
                {
                    Version version = new Version(Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0]);
                    this.applicationVersion = version.ToString(4);
                }

                return this.applicationVersion;
            }
        }

        public string ApplicationFullName
        {
            get
            {
                return string.Format("{0} V{1}", this.ApplicationName, this.ApplicationVersion);
            }
        }

        public SettingsViewModel SettingsViewModel
        {
            get { return this.settingsViewModel; }
        }

        public string[] ChartTypes
        {
            get { return this.chartTypes; }
        }

        [IgnoreDataMember]
        public ObservableCollection<TripModel> Trips
        {
            get
            {
                return this.trips;
            }

            set
            {
                if (this.trips != value)
                {
                    this.trips = value;
                    this.NotifyPropertyChanged("Trips");
                }
            }
        }

        [IgnoreDataMember]
        public ObservableCollection<ObdModel> MapObdUpdates
        {
            get { return this.mapObdUpdates; }
            set
            {
                if (this.mapObdUpdates != value)
                {
                    this.mapObdUpdates = value;
                    this.NotifyPropertyChanged("MapObdUpdates");
                }
            }
        }

        [IgnoreDataMember]
        public ObservableCollection<GeocoordinateModel> Geocoordinates
        {
            get { return this.geocoordinates; }
            set
            {
                if (this.geocoordinates != value)
                {
                    this.geocoordinates = value;
                    this.NotifyPropertyChanged("Geocoordinates");
                }
            }
        }

        [IgnoreDataMember]
        public GeoCoordinateCollection MapPath
        {
            get { return this.mapPath; }
            set
            {
                if (this.mapPath != value)
                {
                    this.mapPath = value;
                    this.NotifyPropertyChanged("MapPath");
                }
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public void Add(GeocoordinateModel newGeocoordinate)
        {
            obdDb.GeocoordinateModels.InsertOnSubmit(newGeocoordinate);

            obdDb.SubmitChanges();

            Geocoordinates.Add(newGeocoordinate);
            MapPath.Add(newGeocoordinate.GeoCoordinate);
        }

        public void Add(ObdModel newObd)
        {
            obdDb.ObdModels.InsertOnSubmit(newObd);

            obdDb.SubmitChanges();

            MapObdUpdates.Add(newObd);
        }

        public void Add(TripModel newTrip)
        {
            obdDb.Trips.InsertOnSubmit(newTrip);

            obdDb.SubmitChanges();

            Trips.Insert(0, newTrip);
        }

        public void Remove(TripModel trip)
        {
            obdDb.Trips.DeleteOnSubmit(trip);
            obdDb.SubmitChanges();

            Trips.Remove(trip);
        }

        public void Clear()
        {
            obdDb.GeocoordinateModels.DeleteAllOnSubmit(Geocoordinates);
            Geocoordinates.Clear();

            obdDb.ObdModels.DeleteAllOnSubmit(MapObdUpdates);
            MapObdUpdates.Clear();

            obdDb.SubmitChanges();
        }

        public void LoadData()
        {
            var geoCoordinatesInDb = from GeocoordinateModel geo in obdDb.GeocoordinateModels
                                     select geo;
            this.Geocoordinates = new ObservableCollection<GeocoordinateModel>(geoCoordinatesInDb);

            foreach (GeocoordinateModel geo in Geocoordinates)
            {
                this.MapPath.Add(geo.GeoCoordinate);
            }

            var obdModelsInDb = from ObdModel obd in obdDb.ObdModels
                                select obd;

            this.MapObdUpdates = new ObservableCollection<ObdModel>(obdModelsInDb);

            var tripsInDb = from TripModel trip in obdDb.Trips
                            orderby trip.TripEnd descending
                            select trip;
            this.Trips = new ObservableCollection<TripModel>(tripsInDb);

            IsolatedStorageSettings.ApplicationSettings.TryGetValue<int>("applicationLoadCount", out applicationLoadCount);
            IsolatedStorageSettings.ApplicationSettings.TryGetValue<bool>("promptedForReview", out promptedForReview);

            this.IsDataLoaded = true;
        }

        private void MapObdUpdatesPropertyChanged()
        {
            this.NotifyPropertyChanged("MapObdUpdates");
        }

        private void MapPathPropertyChanged()
        {
            this.NotifyPropertyChanged("MapPath");
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