namespace Models
{
    using System;
    using System.ComponentModel;
    using System.Data.Linq;
    using System.Data.Linq.Mapping;

    [Table]
    public class TripModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private int id;
        private string name;
        private double startSoC;
        private double endSoC;
        private double kiloWattHoursUsed;
        private double averageEnergyEconomy;
        private double distance;
        private double capacity;
        private double rawCapacity;
        private double ascent;
        private double descent;
        private double? minimumTemperature1;
        private double? minimumTemperature2;
        private double? minimumTemperature3;
        private double? minimumTemperature4;
        private double? maximumTemperature1;
        private double? maximumTemperature2;
        private double? maximumTemperature3;
        private double? maximumTemperature4;
        private double? averageTemperature1;
        private double? averageTemperature2;
        private double? averageTemperature3;
        private double? averageTemperature4;

        private DateTime tripStart;
        private DateTime tripEnd;

        [Column(IsVersion = true)]
        private Binary version;

        public TripModel()
        {
        }

        public TripModel(ObdModel start, ObdModel end)
        {
            this.TripStart = start.When;
            this.TripEnd = end.When;
            this.StartSoC = start.SoC;
            this.EndSoC = end.SoC;
            this.KiloWattHoursUsed = end.TotalKiloWattHoursUsed;
            this.AverageEnergyEconomy = end.AverageEnergyEconomy;
            this.Distance = end.TotalDistance;
            this.Capacity = end.Capacity;
            this.RawCapacity = end.RawCapacity;
            this.Ascent = end.TotalAscent;
            this.Descent = end.TotalDescent;
        }

        [Column(DbType = "INT NOT NULL IDENTITY", IsDbGenerated = true, IsPrimaryKey = true, AutoSync = AutoSync.OnInsert)]
        public int Id
        {
            get { return this.id; }
            set
            {
                this.NotifyPropertyChanging("Id");
                this.id = value;
                this.NotifyPropertyChanged("Id");
            }
        }

        [Column(CanBeNull = true)]
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                if (value != this.name)
                {
                    this.NotifyPropertyChanging("Name");
                    this.name = value;
                    this.NotifyPropertyChanged("Name");
                }
            }
        }

        [Column]
        public double StartSoC
        {
            get { return this.startSoC; }
            set
            {
                this.NotifyPropertyChanging("StartSoC");
                this.startSoC = value;
                this.NotifyPropertyChanged("StartSoC");
            }
        }

        [Column]
        public double EndSoC
        {
            get { return this.endSoC; }
            set
            {
                this.NotifyPropertyChanging("EndSoC");
                this.endSoC = value;
                this.NotifyPropertyChanged("EndSoC");
            }
        }

        [Column]
        public double AverageEnergyEconomy
        {
            get { return this.averageEnergyEconomy; }
            set
            {
                this.NotifyPropertyChanging("AverageEnergyEconomy");
                this.averageEnergyEconomy = value;
                this.NotifyPropertyChanged("AverageEnergyEconomy");
            }
        }

        [Column]
        public double KiloWattHoursUsed
        {
            get { return this.kiloWattHoursUsed; }
            set
            {
                this.NotifyPropertyChanging("KiloWattHoursUsed");
                this.kiloWattHoursUsed = value;
                this.NotifyPropertyChanged("KiloWattHoursUsed");
            }
        }

        [Column]
        public double Distance
        {
            get { return this.distance; }
            set
            {
                this.NotifyPropertyChanging("Distance");
                this.distance = value;
                this.NotifyPropertyChanged("Distance");
            }
        }

        [Column]
        public double Ascent
        {
            get { return this.ascent; }
            set
            {
                this.NotifyPropertyChanging("Ascent");
                this.ascent = value;
                this.NotifyPropertyChanged("Ascent");
            }
        }

        [Column]
        public double Descent
        {
            get { return this.descent; }
            set
            {
                this.NotifyPropertyChanging("Descent");
                this.descent = value;
                this.NotifyPropertyChanged("Descent");
            }
        }

        [Column]
        public double Capacity
        {
            get { return this.capacity; }
            set
            {
                this.NotifyPropertyChanging("Capacity");
                this.capacity = value;
                this.NotifyPropertyChanged("Capacity");
            }
        }

        [Column]
        public double RawCapacity
        {
            get { return this.rawCapacity; }
            set
            {
                this.NotifyPropertyChanging("RawCapacity");
                this.rawCapacity = value;
                this.NotifyPropertyChanged("RawCapacity");
            }
        }

        [Column(CanBeNull = true)]
        public double? MinimumTemperature1
        {
            get { return this.minimumTemperature1; }
            set
            {
                this.NotifyPropertyChanging("MinimumTemperature1");
                this.minimumTemperature1 = value;
                this.NotifyPropertyChanged("MinimumTemperature1");
            }
        }

        [Column(CanBeNull = true)]
        public double? MinimumTemperature2
        {
            get { return this.minimumTemperature2; }
            set
            {
                this.NotifyPropertyChanging("MinimumTemperature2");
                this.minimumTemperature2 = value;
                this.NotifyPropertyChanged("MinimumTemperature2");
            }
        }

        [Column(CanBeNull = true)]
        public double? MinimumTemperature3
        {
            get { return this.minimumTemperature3; }
            set
            {
                this.NotifyPropertyChanging("MinimumTemperature3");
                this.minimumTemperature3 = value;
                this.NotifyPropertyChanged("MinimumTemperature3");
            }
        }

        [Column(CanBeNull = true)]
        public double? MinimumTemperature4
        {
            get { return this.minimumTemperature4; }
            set
            {
                this.NotifyPropertyChanging("MinimumTemperature4");
                this.minimumTemperature4 = value;
                this.NotifyPropertyChanged("MinimumTemperature4");
            }
        }

        [Column(CanBeNull = true)]
        public double? MaximumTemperature1
        {
            get { return this.maximumTemperature1; }
            set
            {
                this.NotifyPropertyChanging("MaximumTemperature1");
                this.maximumTemperature1 = value;
                this.NotifyPropertyChanged("MaximumTemperature1");
            }
        }

        [Column(CanBeNull = true)]
        public double? MaximumTemperature2
        {
            get { return this.maximumTemperature2; }
            set
            {
                this.NotifyPropertyChanging("MaximumTemperature2");
                this.maximumTemperature2 = value;
                this.NotifyPropertyChanged("MaximumTemperature2");
            }
        }

        [Column(CanBeNull = true)]
        public double? MaximumTemperature3
        {
            get { return this.maximumTemperature3; }
            set
            {
                this.NotifyPropertyChanging("MaximumTemperature3");
                this.maximumTemperature3 = value;
                this.NotifyPropertyChanged("MaximumTemperature3");
            }
        }

        [Column(CanBeNull = true)]
        public double? MaximumTemperature4
        {
            get { return this.maximumTemperature4; }
            set
            {
                this.NotifyPropertyChanging("MaximumTemperature4");
                this.maximumTemperature4 = value;
                this.NotifyPropertyChanged("MaximumTemperature4");
            }
        }

        [Column(CanBeNull = true)]
        public double? AverageTemperature1
        {
            get { return this.averageTemperature1; }
            set
            {
                this.NotifyPropertyChanging("AverageTemperature1");
                this.averageTemperature1 = value;
                this.NotifyPropertyChanged("AverageTemperature1");
            }
        }

        [Column(CanBeNull = true)]
        public double? AverageTemperature2
        {
            get { return this.averageTemperature2; }
            set
            {
                this.NotifyPropertyChanging("AverageTemperature2");
                this.averageTemperature2 = value;
                this.NotifyPropertyChanged("AverageTemperature2");
            }
        }

        [Column(CanBeNull = true)]
        public double? AverageTemperature3
        {
            get { return this.averageTemperature3; }
            set
            {
                this.NotifyPropertyChanging("AverageTemperature3");
                this.averageTemperature3 = value;
                this.NotifyPropertyChanged("AverageTemperature3");
            }
        }

        [Column(CanBeNull = true)]
        public double? AverageTemperature4
        {
            get { return this.averageTemperature4; }
            set
            {
                this.NotifyPropertyChanging("AverageTemperature4");
                this.averageTemperature4 = value;
                this.NotifyPropertyChanged("AverageTemperature4");
            }
        }

        public string DisplayMinimumTemperature
        {
            get
            {
                if (this.MinimumTemperature1.HasValue && this.MinimumTemperature2.HasValue && this.MinimumTemperature3.HasValue && this.MinimumTemperature4.HasValue)
                {
                    return string.Format("{0:N0}°, {1:N0}°, {2:N0}°, {3:N0}°", this.MinimumTemperature1, this.MinimumTemperature2, this.MinimumTemperature3, this.MinimumTemperature4);
                }

                return null;
            }
        }

        public string DisplayMaximumTemperature
        {
            get
            {
                if (this.MaximumTemperature1.HasValue && this.MaximumTemperature2.HasValue && this.MaximumTemperature3.HasValue && this.MaximumTemperature4.HasValue)
                {
                    return string.Format("{0:N0}°, {1:N0}°, {2:N0}°, {3:N0}°", this.MaximumTemperature1, this.MaximumTemperature2, this.MaximumTemperature3, this.MaximumTemperature4);
                }

                return null;
            }
        }

        public string DisplayAverageTemperature
        {
            get
            {
                if (this.AverageTemperature1.HasValue && this.AverageTemperature2.HasValue && this.AverageTemperature3.HasValue && this.AverageTemperature4.HasValue)
                {
                    return string.Format("{0:N0}°, {1:N0}°, {2:N0}°, {3:N0}°", this.AverageTemperature1, this.AverageTemperature2, this.AverageTemperature3, this.AverageTemperature4);
                }

                return null;
            }
        }

        [Column]
        public DateTime TripStart
        {
            get { return this.tripStart; }
            set
            {
                this.NotifyPropertyChanging("TripStart");
                this.tripStart = value;
                this.NotifyPropertyChanged("TripStart");
            }
        }

        [Column]
        public DateTime TripEnd
        {
            get { return this.tripEnd; }
            set
            {
                this.NotifyPropertyChanging("TripEnd");
                this.tripEnd = value;
                this.NotifyPropertyChanged("TripEnd");
            }
        }

        public DateTime DisplayTripEnd
        {
            get { return this.tripEnd.ToLocalTime(); }
        }

        public double ChargeUsed
        {
            get { return this.StartSoC - this.EndSoC; }
        }

        public TimeSpan Duration
        {
            get { return this.TripEnd.Subtract(this.TripStart).Duration(); }
        }

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
