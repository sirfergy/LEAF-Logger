namespace Models
{
    using System;
    using System.ComponentModel;
    using System.Data.Linq;
    using System.Data.Linq.Mapping;
    using System.Device.Location;

    [Table]
    public class ObdModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private int id;
        private double soc;
        private double capacity;
        private double rawCapacity;
        private DateTime when;
        private double distance;
        private double totalDistance;
        private double ascent;
        private double descent;
        private double totalAscent;
        private double totalDescent;
        private double kiloWattHoursUsed;
        private double totalKiloWattHoursUsed;

        // Temperature
        private double? temperature1;
        private double? temperature2;
        private double? temperature3;
        private double? temperature4;

        // GeoCoordinate properties
        private GeoCoordinate geoCoordinate;
        private bool hasLocationInfo;
        private double latitude;
        private double longitude;
        private double speed;
        private double altitude;
        private double course;
        private double horizontalAccuracy;
        private double verticalAccuracy;

        [Column(IsVersion = true)]
        private Binary version;

        public ObdModel()
        {
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
        public double SoC
        {
            get
            {
                return this.soc;
            }

            set
            {
                if (value != this.soc)
                {
                    this.NotifyPropertyChanging("SoC");
                    this.soc = value;
                    this.NotifyPropertyChanged("SoC");
                }
            }
        }

        public double SoCOffset
        {
            get
            {
                return this.SoC + 0.001;
            }
        }

        [Column]
        public double Capacity
        {
            get
            {
                return this.capacity;
            }

            set
            {
                if (value != this.capacity)
                {
                    this.NotifyPropertyChanging("Capacity");
                    this.capacity = value;
                    this.NotifyPropertyChanged("Capacity");
                }
            }
        }

        [Column]
        public double RawCapacity
        {
            get
            {
                return this.rawCapacity;
            }

            set
            {
                if (value != this.rawCapacity)
                {
                    this.NotifyPropertyChanging("RawCapacity");
                    this.rawCapacity = value;
                    this.NotifyPropertyChanged("RawCapacity");
                }
            }
        }

        public double CapacityOffset
        {
            get
            {
                return this.Capacity + 0.001;
            }
        }

        [Column]
        public double KiloWattHoursUsed
        {
            get
            {
                return this.kiloWattHoursUsed;
            }

            set
            {
                if (value != this.kiloWattHoursUsed)
                {
                    this.NotifyPropertyChanging("KiloWattHoursUsed");
                    this.kiloWattHoursUsed = value;
                    this.NotifyPropertyChanged("KiloWattHoursUsed");
                }
            }
        }

        [Column]
        public double TotalKiloWattHoursUsed
        {
            get
            {
                return this.totalKiloWattHoursUsed;
            }

            set
            {
                if (value != this.totalKiloWattHoursUsed)
                {
                    this.NotifyPropertyChanging("TotalKiloWattHoursUsed");
                    this.totalKiloWattHoursUsed = value;
                    this.NotifyPropertyChanged("TotalKiloWattHoursUsed");
                }
            }
        }

        public double UsableKiloWattHours
        {
            get
            {
                return this.Capacity * 24;
            }
        }

        public double AvailableKiloWattHours
        {
            get
            {
                return this.SoC * this.UsableKiloWattHours;
            }
        }

        [Column]
        public DateTime When
        {
            get
            {
                return this.when;
            }

            set
            {
                if (value != this.when)
                {
                    this.NotifyPropertyChanging("When");
                    this.when = value;
                    this.NotifyPropertyChanged("When");
                }
            }
        }

        public DateTime DisplayWhen
        {
            get
            {
                return this.When.ToLocalTime();
            }
        }

        public double AverageEnergyEconomy
        {
            get
            {
                if (this.TotalDistance > 0 &&
                    this.TotalKiloWattHoursUsed > 0)
                {
                    return this.TotalDistance / this.TotalKiloWattHoursUsed;
                }
                else
                {
                    return 0;
                }
            }
        }

        public double Range
        {
            get
            {
                return this.AverageEnergyEconomy * this.AvailableKiloWattHours;
            }
        }

        [Column]
        public double Distance
        {
            get
            {
                return this.distance;
            }

            set
            {
                if (value != this.distance)
                {
                    this.NotifyPropertyChanging("Distance");
                    this.distance = value;
                    this.NotifyPropertyChanged("Distance");
                }
            }
        }

        [Column]
        public double TotalDistance
        {
            get
            {
                return this.totalDistance;
            }

            set
            {
                if (value != this.totalDistance)
                {
                    this.NotifyPropertyChanging("TotalDistance");
                    this.totalDistance = value;
                    this.NotifyPropertyChanged("TotalDistance");
                }
            }
        }

        public string TotalDistanceRounded
        {
            get
            {
                return Math.Round(this.TotalDistance, 1) + " mi";
            }
        }

        public double MilesPerHour
        {
            get
            {
                return this.Speed * 2.23693629;
            }
        }

        public double KilometersPerHour
        {
            get
            {
                return this.Speed * 3.6;
            }
        }


        public double Elevation
        {
            get
            {
                return this.Altitude * 3.2808399;
            }
        }

        [Column]
        public double Ascent
        {
            get
            {
                return this.ascent;
            }

            set
            {
                if (value != this.ascent)
                {
                    this.NotifyPropertyChanging("Ascent");
                    this.ascent = value;
                    this.NotifyPropertyChanged("Ascent");
                }
            }
        }

        [Column]
        public double Descent
        {
            get
            {
                return this.descent;
            }

            set
            {
                if (value != this.descent)
                {
                    this.NotifyPropertyChanging("Descent");
                    this.descent = value;
                    this.NotifyPropertyChanged("Descent");
                }
            }
        }

        [Column]
        public double TotalAscent
        {
            get
            {
                return this.totalAscent;
            }

            set
            {
                if (value != this.totalAscent)
                {
                    this.NotifyPropertyChanging("TotalAscent");
                    this.totalAscent = value;
                    this.NotifyPropertyChanged("TotalAscent");
                }
            }
        }

        [Column]
        public double TotalDescent
        {
            get
            {
                return this.totalDescent;
            }

            set
            {
                if (value != this.totalDescent)
                {
                    this.NotifyPropertyChanging("TotalDescent");
                    this.totalDescent = value;
                    this.NotifyPropertyChanged("TotalDescent");
                }
            }
        }

        [Column]
        public bool HasLocationInfo
        {
            get
            {
                return this.hasLocationInfo;
            }

            set
            {
                if (value != this.hasLocationInfo)
                {
                    this.NotifyPropertyChanging("HasLocationInfo");
                    this.hasLocationInfo = value;
                    this.NotifyPropertyChanged("HasLocationInfo");
                }
            }
        }

        #region Temperature
        [Column(CanBeNull = true)]
        public double? Temperature1
        {
            get
            {
                return this.temperature1;
            }

            set
            {
                if (value != this.temperature1)
                {
                    this.NotifyPropertyChanging("Temperature1");
                    this.temperature1 = value;
                    this.NotifyPropertyChanged("Temperature1");
                }
            }
        }

        [Column(CanBeNull = true)]
        public double? Temperature2
        {
            get
            {
                return this.temperature2;
            }

            set
            {
                if (value != this.temperature2)
                {
                    this.NotifyPropertyChanging("Temperature2");
                    this.temperature2 = value;
                    this.NotifyPropertyChanged("Temperature2");
                }
            }
        }

        [Column(CanBeNull = true)]
        public double? Temperature3
        {
            get
            {
                return this.temperature3;
            }

            set
            {
                if (value != this.temperature3)
                {
                    this.NotifyPropertyChanging("Temperature3");
                    this.temperature3 = value;
                    this.NotifyPropertyChanged("Temperature3");
                }
            }
        }

        [Column(CanBeNull = true)]
        public double? Temperature4
        {
            get
            {
                return this.temperature4;
            }

            set
            {
                if (value != this.temperature4)
                {
                    this.NotifyPropertyChanging("Temperature4");
                    this.temperature4 = value;
                    this.NotifyPropertyChanged("Temperature4");
                }
            }
        }

        public string DisplayTemperature
        {
            get
            {
                if (this.Temperature1.HasValue && this.Temperature2.HasValue && this.Temperature3.HasValue && this.Temperature4.HasValue)
                {
                    return string.Format("{0:N0}°, {1:N0}°, {2:N0}°, {3:N0}°", this.Temperature1, this.Temperature2, this.Temperature3, this.Temperature4);
                }

                return null;
            }
        }
        #endregion

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

                    this.HasLocationInfo = true;
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
        private void NotifyPropertyChanged(string propertyName)
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
