using Newtonsoft.Json;
using System.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using ParkingLibrary;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Timers;
using Dapper;
using LatLng = ParkingLibrary.LatLng;

namespace ParkingInfo
{
    class Program
    {
        private static Timer timer1;
        private static Timer timer2;

        static void Main(string[] args)
        {
            SetTimer();
            //Thread thread = new Thread(DoWork);
            //thread.Start();

            //Task.Run(() =>
            //{
            //    DoDailyWork();

            //});

            //Task.Run(() =>
            //{
            //    DoFiveMinsWork();

            //});
            Console.ReadKey();
        }

        private static void SetTimer()
        {
            timer1 = new System.Timers.Timer(50000000);
            timer1.Elapsed += OnTimedEvent1;
            timer1.AutoReset = true; //每次達到指定間隔時間後，就觸發事件
            timer1.Enabled = true; //啟動計時器
            timer2 = new System.Timers.Timer(5000);
            timer2.Elapsed += OnTimedEvent2;
            timer2.AutoReset = true;
            timer2.Enabled = true;
        }

        private static void OnTimedEvent1(Object source, System.Timers.ElapsedEventArgs e)
        {
            DoFiveMinsWork();
        }

        private static void OnTimedEvent2(Object source, System.Timers.ElapsedEventArgs e)
        {
            DoDailyWork();
        }

        private static void DoFiveMinsWork()
        {
            ParkingClass1 condb = new ParkingClass1();
            var col = condb.MongodbConn("ParkingSpace", "parking_info");
            var documents = col.Find(new BsonDocument()).Project("{Id: 1, AvailableCar: 1, _id: 0}").ToList();
            for (int i = 0; i < 2; i++)
            {
                string parkingSpace = GetJson("https://data.ntpc.gov.tw/api/datasets/E09B35A5-A738-48CC-B0F5-570B67AD9C78/json?page=" + i + "&size=1000");
                List<ParkingCategoryContainer> data = JsonConvert.DeserializeObject<List<ParkingCategoryContainer>>(parkingSpace);
                foreach (ParkingCategoryContainer item in data)
                {
                    BsonDocument doc = new BsonDocument { { "Id", item.Id }, { "AvailableCar", item.AvailableCar }, { "UpdateTime", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") } };
                    if (documents.Count != 0)
                    {
                        bool repeat = false;
                        foreach (var key in documents)
                        {
                            if (key.GetValue("Id").AsString == item.Id)
                            {
                                var filter = Builders<BsonDocument>.Filter.Eq("Id", item.Id);
                                var update = Builders<BsonDocument>.Update.Set("AvailableCar", item.AvailableCar).Set("UpdateTime", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                                col.UpdateMany(filter, update);
                                repeat = true;
                                break;
                            }
                        }
                        if (repeat == false) 
                        {
                            col.InsertOne(doc);
                        }
                    }
                    else 
                    {
                        col.InsertOne(doc);
                    }

                }

            }
        }

        private static void DoDailyWork()
        {
            ParkingClass1 condb = new ParkingClass1();
            string searchLonLat = @"select lon, lat from parking_info;";
            List<ParkingData> info = condb.Query(searchLonLat, null);
            List<LatLng> latlng = new List<LatLng>();
            foreach (var item in info)
            {
                latlng.Add(new LatLng { Lon = item.Lon, Lat = item.Lat });
            }
            int num = latlng.Count;
            for (int i = 0; i < 2; i++)
            {
                string parkingData = GetJson("https://data.ntpc.gov.tw/api/datasets/B1464EF0-9C7C-4A6F-ABF7-6BDF32847E68/json?page=" + i + "&size=1000");
                //將json資料轉存到類別物件中
                List<ParkingDataContainer> data = JsonConvert.DeserializeObject<List<ParkingDataContainer>>(parkingData);
                CoordinateTransform CoordinateTransform = new CoordinateTransform();
                string updateInfo = @"UPDATE parking_info 
                                 SET name=@name,area=@area,address=@address,serviceTime=@serviceTime,payEX=@payEx
                                 ,totalCar=@totalCar,totalMotor=@totalMotor,summary=@summary,id=@id,tel=@tel
                                 ,updateTime=convert(varchar, getdate(), 120) 
                                 WHERE lon=@lon and lat=@lat;";
                string insertInfo = @"INSERT INTO parking_info (lon, lat, name, area, address, serviceTime, payEX, totalCar, totalMotor, summary, id, tel, updateTime) 
                                 VALUES (@lon,@lat,@name,@area,@address,@serviceTime,@payEx,@totalCar,@totalMotor,@summary,@id,@tel,convert(varchar, getdate(), 120));";
                string executeString;
                foreach (ParkingDataContainer item in data)
                {
                    //將TWD97座標轉為一般經緯度座標
                    string lonlat = CoordinateTransform.TWD97_To_lonlat(Convert.ToDouble(item.Twd97X), Convert.ToDouble(item.Twd97Y), 2);
                    string[] lonlatArray = lonlat.Split(',');
                    string lon = lonlatArray[0];
                    string lat = lonlatArray[1];

                    if (num != 0) //若資料庫有資料
                    {
                        bool repeat = false;
                        //判斷資料庫是否有相同經緯度的資料
                        foreach (LatLng loc in latlng)
                        {
                            if (loc.Lon == lon && loc.Lat == lat)
                            {
                                repeat = true;
                                break;
                            }
                        }

                        if (repeat == true)
                        {
                            executeString = updateInfo;
                        }
                        else
                        {
                            executeString = insertInfo;
                        }

                    }
                    else //資料庫沒有資料直接新增
                    {
                        executeString = insertInfo;
                    }
                    try
                    {
                        DynamicParameters parameters = new DynamicParameters();
                        parameters.Add("@lon", lon);
                        parameters.Add("@lat", lat);
                        parameters.Add("@name", item.Name);
                        parameters.Add("@area", item.Area);
                        parameters.Add("@address", item.Address);
                        parameters.Add("@serviceTime", item.ServiceTime);
                        parameters.Add("@payEx", item.PayEx);
                        parameters.Add("@totalCar", item.TotalCar);
                        parameters.Add("@totalMotor", item.TotalMotor);
                        parameters.Add("@summary", item.Summary);
                        parameters.Add("@id", item.Id);
                        parameters.Add("@tel", item.Tel);
                        condb.DapperExecute(executeString, parameters);
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine(ex.Message); //查看SqlException錯誤內容
                    }
                }
            }
            //ParkingClass1 condb = new ParkingClass1();
            //string searchLonLat = @"select lon, lat from parking_info;";
            //List<LatLng> latlng = new List<LatLng>();
            //SqlDataReader dr = condb.ReadData(searchLonLat, null);
            //while (dr.Read()) //讀資料庫的經緯度
            //{
            //    latlng.Add(new LatLng { Lon = dr[0].ToString(), Lat = dr[1].ToString() });
            //}
            //dr.Close();
            //condb.CloseDB();
            //int num = latlng.Count;

            //for (int i = 0; i < 2; i++)
            //{
            //    string parkingData = GetJson("https://data.ntpc.gov.tw/api/datasets/B1464EF0-9C7C-4A6F-ABF7-6BDF32847E68/json?page=" + i + "&size=1000");
            //    //將json資料轉存到類別物件中
            //    List<ParkingData> data = JsonConvert.DeserializeObject<List<ParkingData>>(parkingData);
            //    CoordinateTransform CoordinateTransform = new CoordinateTransform();
            //    string updateInfo = @"UPDATE parking_info SET name=@name,area=@area,address=@address,
            //                        serviceTime=@serviceTime,payEX=@payEx,totalCar=@totalCar,totalMotor=@totalMotor,
            //                        summary=@summary,id=@id,tel=@tel,updateTime=convert(varchar, getdate(), 120) 
            //                        WHERE lon=@lon and lat=@lat;";
            //    string insertInfo = @"INSERT INTO parking_info (lon, lat, name, area, address, serviceTime, payEX, totalCar, totalMotor, summary, id, tel, updateTime) 
            //                     VALUES (@lon,@lat,@name,@area,@address,@serviceTime,@payEx,@totalCar,@totalMotor,@summary,@id,@tel,convert(varchar, getdate(), 120));";
            //    string executeString;
            //    foreach (ParkingData item in data)
            //    {
            //        //將TWD97座標轉為一般經緯度座標
            //        string lonlat = CoordinateTransform.TWD97_To_lonlat(Convert.ToDouble(item.Twd97X), Convert.ToDouble(item.Twd97Y), 2);
            //        string[] lonlatArray = lonlat.Split(',');
            //        string lon = lonlatArray[0];
            //        string lat = lonlatArray[1];
            //        Console.WriteLine("lon:" + lon + "lat:" + lat);
            //        if (num != 0) //若資料庫有資料
            //        {
            //            bool repeat = false;
            //            //判斷資料庫是否有相同經緯度的資料
            //            foreach (LatLng loc in latlng)
            //            {
            //                if (loc.Lon == lon && loc.Lat == lat)
            //                {
            //                    repeat = true;
            //                    break;
            //                }
            //            }

            //            if (repeat == true)
            //            {
            //                executeString = updateInfo;
            //            }
            //            else
            //            {
            //                executeString = insertInfo;
            //            }

            //        }
            //        else //資料庫沒有資料直接新增
            //        {
            //            executeString = insertInfo;
            //        }
            //        try
            //        {
            //            SqlParameter[] param = new SqlParameter[]
            //            {
            //                new SqlParameter("@lon", lon),
            //                new SqlParameter("@lat", lat),
            //                new SqlParameter("@name", item.Name),
            //                new SqlParameter("@area", item.Area),
            //                new SqlParameter("@address", item.Address),
            //                new SqlParameter("@serviceTime", item.ServiceTime),
            //                new SqlParameter("@payEx", ((object)item.PayEx) ?? DBNull.Value),
            //                new SqlParameter("@totalCar", item.TotalCar),
            //                new SqlParameter("@totalMotor", item.TotalMotor),
            //                new SqlParameter("@summary", ((object)item.Summary) ?? DBNull.Value),
            //                new SqlParameter("@id", item.Id),
            //                new SqlParameter("@tel", ((object)item.Id) ?? DBNull.Value)
            //            };
            //            condb.Execute(executeString, param);
            //        }
            //        catch (SqlException ex)
            //        {
            //            Console.WriteLine(ex.Message); //查看SqlException錯誤內容
            //        }
            //    }
            //}
            //condb.CloseDB();
        }


        public static string GetJson(string Url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url); //request請求
            request.Timeout = 10000; //request逾時時間
            request.Method = "GET"; //request方式
            HttpWebResponse respone = (HttpWebResponse)request.GetResponse(); //接收respone
            StreamReader streamReader = new StreamReader(respone.GetResponseStream(), Encoding.UTF8); //讀取respone資料
            string result = streamReader.ReadToEnd(); //讀取到最後一行
            respone.Close();
            streamReader.Close();
            //JArray jsondata = JsonConvert.DeserializeObject<JArray>(result); //將資料轉為json陣列
            //return jsondata; 
            return result;
        }

    }
}
