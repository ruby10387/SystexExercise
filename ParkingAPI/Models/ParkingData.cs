using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingInfo
{
    public class ParkingData
    {
        public string Id { get; set; }

        public string Area { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string Summary { get; set; }

        public string Address { get; set; }

        public string Tel { get; set; }

        public string PayEx { get; set; }

        public string ServiceTime { get; set; }

        public string Lon { get; set; }

        public string Lat { get; set; }

        public string TotalCar { get; set; }

        public string TotalMotor { get; set; }

        public object TotalBike { get; set; }
        public object AvailableCar { get; internal set; }
    }

}
