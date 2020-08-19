using Dapper;
using MongoDB.Bson;
using MongoDB.Driver;
using ParkingLibrary;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Web.Http;
using ParkingData = ParkingLibrary.ParkingData;
using LatLng = ParkingLibrary.LatLng;

namespace ParkingAPI.Controllers
{
    public class ParkingInfoController : ApiController
    {
        ParkingClass1 condb = new ParkingClass1();
        public List<ParkingData> Get(string area)
        {
            //data source這裡需要兩條\\、web.config不用
            //string connectionString = "Integrated Security=true; data source=(localdb)\\mssqllocaldb; initial catalog = PARKING;";
            //SqlConnection conn = new SqlConnection(connectionString);

            string searchInfo = @"select * from parking_info where area = @area;";
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@area", new DbString() { Value = area, IsAnsi = false, IsFixedLength = false, Length = 10 });
            List<ParkingData> data = condb.Query(searchInfo, parameters);
            foreach (var item in data)
            {
                item.AvailableCar = GetAvailableCar(item.Id);
            }
            return data;
            //string searchInfo = @"select * from parking_info where area = @area;";
            //SqlParameter[] param = new SqlParameter[]
            //{
            //    new SqlParameter("@area",area)
            //};
            //List<ParkingData> data = new List<ParkingData>();
            //SqlDataReader dr = condb.ReadData(searchInfo, param);
            //while (dr.Read())
            //{
            //    data.Add(new ParkingData
            //    {
            //        Lon = dr[0].ToString(),
            //        Lat = dr[1].ToString(),
            //        Name = dr[2].ToString(),
            //        Area = dr[3].ToString(),
            //        Address = dr[4].ToString(),
            //        ServiceTime = dr[5].ToString(),
            //        PayEx = dr[6].ToString(),
            //        TotalCar = dr[7].ToString(),
            //        TotalMotor = dr[8].ToString(),
            //        Summary = dr[9].ToString(),
            //        Id = dr[10].ToString(),
            //        Tel = dr[11].ToString(),
            //        AvailableCar = getAvailableCar(dr[10].ToString())
            //    });
            //}
            //dr.Close();
            //condb.CloseDB();
            //return data;
        }

        private string GetAvailableCar(string id)
        {
            var AvailableCar = "NULL";
            var col = condb.MongodbConn("ParkingSpace", "parking_info");
            var parkingSpace = col.Find(new BsonDocument()).Project("{Id: 1, AvailableCar: 1, _id: 0}").ToList();

            AvailableCar = (string)parkingSpace
                .Where(x => x.GetValue("Id") == id && x.GetValue("AvailableCar").AsString != "-9")
                .Select(x => x.GetValue("AvailableCar")).ToList()
                .FirstOrDefault();
            //foreach (var key in parkingSpace)
            //{
            //    if (key.GetValue("Id").AsString == id && Int32.Parse(key.GetValue("AvailableCar").AsString) >= 0)
            //    {
            //        AvailableCar = key.GetValue("AvailableCar").AsString;
            //        break;
            //    }
            //}
            return AvailableCar;
        }

        public List<ParkingData> Get(double lon, double lat, double dist)
        {
            string searchLonLat = @"select lon, lat from parking_info;";
            List<ParkingData> data = condb.Query(searchLonLat, null);
            List<LatLng> latlng = new List<LatLng>();
            foreach (var item in data)
            {
                latlng.Add(new LatLng { Lon = item.Lon, Lat = item.Lat });
            }
            string searchInfo = @"select * from parking_info where lon = @lon and lat = @lat;";
            List<ParkingData> data2 = new List<ParkingData>();
            foreach (LatLng item in latlng)
            {
                double longitude = Convert.ToDouble(item.Lon);
                double latitude = Convert.ToDouble(item.Lat);
                var sCoord = new GeoCoordinate(lat, lon);
                var dCoord = new GeoCoordinate(latitude, longitude);
                if (sCoord.GetDistanceTo(dCoord) / 1000 <= dist) //若計算兩個經緯度的距離小於等於欲查詢公里數
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@lon", longitude);
                    parameters.Add("@lat", latitude);
                    data2.AddRange(condb.Query(searchInfo, parameters));
                    foreach (var obj in data2)
                    {
                        obj.AvailableCar = GetAvailableCar(obj.Id);
                    }
                }

            }
            return data2;
            //string searchLonLat = @"select lon, lat from parking_info;";
            //List<LatLng> latlng = new List<LatLng>();
            //List<ParkingData> data = new List<ParkingData>();
            //SqlDataReader dr = condb.ReadData(searchLonLat, null);
            //while (dr.Read()) //讀資料庫的經緯度
            //{
            //    latlng.Add(new LatLng { Lon = dr[0].ToString(), Lat = dr[1].ToString() });
            //}
            //dr.Close();

            //string searchInfo = @"select * from parking_info where lon = @lon and lat = @lat;";
            //foreach (LatLng item in latlng)
            //{
            //    double longitude = Convert.ToDouble(item.Lon);
            //    double latitude = Convert.ToDouble(item.Lat);
            //    var sCoord = new GeoCoordinate(lat, lon);
            //    var dCoord = new GeoCoordinate(latitude, longitude);
            //    if (sCoord.GetDistanceTo(dCoord) / 1000 <= dist) //若計算兩個經緯度的距離小於等於欲查詢公里數
            //    {
            //        SqlParameter[] param = new SqlParameter[]
            //        {
            //            new SqlParameter("@lon",longitude),new SqlParameter("@lat",latitude)
            //        };
            //        SqlDataReader dr2 = condb.ReadData(searchInfo, param);
            //        while (dr2.Read())
            //        {
            //            data.Add(new ParkingData
            //            {
            //                Lon = dr2[0].ToString(),
            //                Lat = dr2[1].ToString(),
            //                Name = dr2[2].ToString(),
            //                Area = dr2[3].ToString(),
            //                Address = dr2[4].ToString(),
            //                ServiceTime = dr2[5].ToString(),
            //                PayEx = dr2[6].ToString(),
            //                TotalCar = dr2[7].ToString(),
            //                TotalMotor = dr2[8].ToString(),
            //                Summary = dr2[9].ToString(),
            //                Id = dr2[10].ToString(),
            //                Tel = dr2[11].ToString(),
            //                AvailableCar = getAvailableCar(dr2[10].ToString())
            //            });
            //        }
            //        dr2.Close();
            //    }

            //}
            //condb.CloseDB();
            //return data;
        }

    }
}
