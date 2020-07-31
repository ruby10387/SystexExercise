using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingInfo
{
    class CoordinateTransform
    {
        double a = 6378137.0;
        double b = 6356752.314245;
        double lon0 = 121 * Math.PI / 180;
        double k0 = 0.9999;
        int dx = 250000;

        public CoordinateTransform()
        {
            //
            // TODO: 在此加入建構函式的程式碼
            //
        }

        public string TWD97_To_lonlat(double XValue, double YValue, int Type)
        {
            string lonlat = "";

            if (Type == 1)
            {
                string[] Answer = Cal_TWD97_To_lonlat(XValue, YValue).Split(',');
                int LonDValue = (int)double.Parse(Answer[0]);
                int LonMValue = (int)((double.Parse(Answer[0]) - LonDValue) * 60);
                int LonSValue = (int)((((double.Parse(Answer[0]) - LonDValue) * 60) - LonMValue) * 60);

                int LatDValue = (int)double.Parse(Answer[1]);
                int LatMValue = (int)((double.Parse(Answer[1]) - LatDValue) * 60);
                int LatSValue = (int)((((double.Parse(Answer[1]) - LatDValue) * 60) - LatMValue) * 60);

                lonlat = LonDValue + "度" + LonMValue + "分" + LonSValue + "秒," + LatDValue + "度" + LatMValue + "分" + LatSValue + "秒,";
            }
            else if (Type == 2)
            {
                lonlat = Cal_TWD97_To_lonlat(XValue, YValue);
            }

            return lonlat;
        }

        private string Cal_TWD97_To_lonlat(double x, double y)
        {

            double dy = 0;
            double e = Math.Pow((1 - Math.Pow(b, 2) / Math.Pow(a, 2)), 0.5);

            x -= dx;
            y -= dy;

            // Calculate the Meridional Arc
            double M = y / k0;

            // Calculate Footprint Latitude
            double mu = M / (a * (1.0 - Math.Pow(e, 2) / 4.0 - 3 * Math.Pow(e, 4) / 64.0 - 5 * Math.Pow(e, 6) / 256.0));
            double e1 = (1.0 - Math.Pow((1.0 - Math.Pow(e, 2)), 0.5)) / (1.0 + Math.Pow((1.0 - Math.Pow(e, 2)), 0.5));

            double J1 = (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32.0);
            double J2 = (21 * Math.Pow(e1, 2) / 16 - 55 * Math.Pow(e1, 4) / 32.0);
            double J3 = (151 * Math.Pow(e1, 3) / 96.0);
            double J4 = (1097 * Math.Pow(e1, 4) / 512.0);

            double fp = mu + J1 * Math.Sin(2 * mu) + J2 * Math.Sin(4 * mu) + J3 * Math.Sin(6 * mu) + J4 * Math.Sin(8 * mu);

            // Calculate Latitude and Longitude

            double e2 = Math.Pow((e * a / b), 2);
            double C1 = Math.Pow(e2 * Math.Cos(fp), 2);
            double T1 = Math.Pow(Math.Tan(fp), 2);
            double R1 = a * (1 - Math.Pow(e, 2)) / Math.Pow((1 - Math.Pow(e, 2) * Math.Pow(Math.Sin(fp), 2)), (3.0 / 2.0));
            double N1 = a / Math.Pow((1 - Math.Pow(e, 2) * Math.Pow(Math.Sin(fp), 2)), 0.5);

            double D = x / (N1 * k0);

            // 計算緯度
            double Q1 = N1 * Math.Tan(fp) / R1;
            double Q2 = (Math.Pow(D, 2) / 2.0);
            double Q3 = (5 + 3 * T1 + 10 * C1 - 4 * Math.Pow(C1, 2) - 9 * e2) * Math.Pow(D, 4) / 24.0;
            double Q4 = (61 + 90 * T1 + 298 * C1 + 45 * Math.Pow(T1, 2) - 3 * Math.Pow(C1, 2) - 252 * e2) * Math.Pow(D, 6) / 720.0;
            double lat = fp - Q1 * (Q2 - Q3 + Q4);

            // 計算經度
            double Q5 = D;
            double Q6 = (1 + 2 * T1 + C1) * Math.Pow(D, 3) / 6;
            double Q7 = (5 - 2 * C1 + 28 * T1 - 3 * Math.Pow(C1, 2) + 8 * e2 + 24 * Math.Pow(T1, 2)) * Math.Pow(D, 5) / 120.0;
            double lon = lon0 + (Q5 - Q6 + Q7) / Math.Cos(fp);

            lat = (lat * 180) / Math.PI; //緯
            lon = (lon * 180) / Math.PI; //經


            string lonlat = lon + "," + lat;
            return lonlat;
        }
    }
}
