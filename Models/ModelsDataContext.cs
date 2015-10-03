namespace Models
{
    using System.Data.Linq;

    public class ModelsDataContext : DataContext
    {
        public ModelsDataContext(string connectionString)
            : base(connectionString)
        { }

        public Table<GeocoordinateModel> GeocoordinateModels;

        public Table<ObdModel> ObdModels;

        public Table<TripModel> Trips;
    }
}
