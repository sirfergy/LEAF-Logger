namespace Models
{
    using System;
    using System.ComponentModel;
    using System.Data.Linq;
    using System.Data.Linq.Mapping;
    using System.Device.Location;
    using Windows.Devices.Geolocation;

    [Table]
    public class GeocoordinateModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private int id;
        private GeoCoordinate geoCoordinate;
        private double latitude;
        private double longitude;
        private double speed;
        private double altitude;
        private double course;
        private double horizontalAccuracy;
        private double verticalAccuracy;
        private DateTime timestamp;

        [Column(IsVersion = true)]
        private Binary version;

        public GeocoordinateModel()
        {
        }

        public GeocoordinateModel(Geocoordinate geocoordinate)
        {
            this.GeoCoordinate = geocoordinate.ToGeoCoordinate();
            this.Timestamp = geocoordinate.Timestamp.UtcDateTime;
        }

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int Id
        {
            get
            {
                return this.id;
            }

            set
            {
                if (id != value)
                {
                    NotifyPropertyChanging("Id");
                    this.id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }

        [Column]
        public DateTime Timestamp
        {
            get { return this.timestamp; }
            set
            {
                if (this.timestamp != value)
                {
                    NotifyPropertyChanging("Timestamp");
                    this.timestamp = value;
                    NotifyPropertyChanged("Timestamp");
                }
            }
        }

        public DateTime DisplayTimestamp
        {
            get { return this.Timestamp.ToLocalTime(); }
        }

        #region GeoCoordinate Properties
        [Column]
        public double Latitude
        {
            get
            {
                return this.latitude;
            }

            set
            {
                if (value != this.latitude)
                {
                    this.NotifyPropertyChanging("Latitude");
                    this.latitude = value;
                    this.NotifyPropertyChanged("Latitude");
                }
            }
        }

        [Column]
        public double Longitude
        {
            get
            {
                return this.longitude;
            }

            set
            {
                if (value != this.longitude)
                {
                    this.NotifyPropertyChanging("Longitude");
                    this.longitude = value;
                    this.NotifyPropertyChanged("Longitude");
                }
            }
        }

        [Column]
        public double Speed
        {
            get
            {
                return this.speed;
            }

            set
            {
                if (value != this.speed)
                {
                    this.NotifyPropertyChanging("Speed");
                    this.speed = value;
                    this.NotifyPropertyChanged("Speed");
                }
            }
        }

        [Column]
        public double Altitude
        {
            get
            {
                return this.altitude;
            }

            set
            {
                if (value != this.altitude)
                {
                    this.NotifyPropertyChanging("Altitude");
                    this.altitude = value;
                    this.NotifyPropertyChanged("Altitude");
                }
            }
        }

        [Column]
        public double Course
        {
            get
            {
                return this.course;
            }

            set
            {
                if (value != this.course)
                {
                    this.NotifyPropertyChanging("Course");
                    this.course = value;
                    this.NotifyPropertyChanged("Course");
                }
            }
        }

        [Column]
        public double HorizontalAccuracy
        {
            get
            {
                return this.horizontalAccuracy;
            }

            set
            {
                if (value != this.horizontalAccuracy)
                {
                    this.NotifyPropertyChanging("HorizontalAccuracy");
                    this.horizontalAccuracy = value;
                    this.NotifyPropertyChanged("HorizontalAccuracy");
                }
            }
        }

        [Column]
        public double VerticalAccuracy
        {
            get
            {
                return this.verticalAccuracy;
            }

            set
            {
                if (value != this.verticalAccuracy)
                {
                    this.NotifyPropertyChanging("VerticalAccuracy");
                    this.verticalAccuracy = value;
                    this.NotifyPropertyChanged("VerticalAccuracy");
                }
            }
        }

        public GeoCoordinate GeoCoordinate
        {
            get
            {
                if (this.geoCoordinate == null)
                {
                    this.geoCoordinate = new GeoCoordinate(
                        latitude: this.Latitude,
                        longitude: this.Longitude,
                        altitude: this.Altitude,
                        horizontalAccuracy: this.HorizontalAccuracy,
                        verticalAccuracy: this.VerticalAccuracy,
                        speed: this.Speed,
                        course: this.Course);
                }

                return this.geoCoordinate;
            }

            set
            {
                if (value != null)
                {
                    this.geoCoordinate = value;

                    this.Latitude = value.Latitude;
                    this.Longitude = value.Longitude;
                    this.Altitude = value.Altitude;
                    this.HorizontalAccuracy = value.HorizontalAccuracy;
                    this.VerticalAccuracy = value.VerticalAccuracy;
                    this.Speed = value.Speed;
                    this.Course = value.Course;
                }
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangingEventHandler PropertyChanging;
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }
    }
}
