using System;
using System.Globalization;
using System.Text;

namespace AirportsPlugin
{
    internal class Airport
    {
        public Airport() { }

        public Airport(string icao, string name, string latitude, string longitude)
        {
            ICAO = icao;
            Name = name.Replace("&", "&amp;");

            var latOK = double.TryParse(latitude, out var latConvert);

            if (latOK) Latitude = latConvert;

            var longOK = double.TryParse(longitude, out var longConvert);

            if (longOK) Longitude = longConvert;

            GetDMS(out var latDeg, out var latMin, out var latSec, out var north, out var lonDeg, out var lonMin, out var lonSec, out var east);

            StringBuilder stringBuilder = new StringBuilder();
            NumberFormatInfo invariantInfo = NumberFormatInfo.InvariantInfo;
            stringBuilder.AppendFormat(invariantInfo, "{0}{1:0#}{2:0#}{3:0#.000#}", north ? "+" : "-", latDeg, latMin, latSec);
            stringBuilder.AppendFormat(invariantInfo, "{0}{1:0##}{2:0#}{3:0#.000#}/", east ? "+" : "-", lonDeg, lonMin, lonSec);
            IsoString = stringBuilder.ToString();
        }

        public string ICAO { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string IsoString { get; set; }

        public void GetDMS(out float latDeg, out float latMin, out float latSec, out bool north, out float lonDeg, out float lonMin, out float lonSec, out bool east)
        {
            double latitude = Latitude;
            north = latitude >= 0.0;
            double num = Math.Abs(latitude);
            latDeg = (float)Math.Truncate(num);
            num -= (double)latDeg;
            num *= 60.0;
            latMin = (float)Math.Truncate(num);
            num -= (double)latMin;
            num *= 60.0;
            latSec = (float)num;
            double longitude = Longitude;
            east = longitude >= 0.0;
            double num2 = Math.Abs(longitude);
            lonDeg = (float)Math.Truncate(num2);
            num2 -= (double)lonDeg;
            num2 *= 60.0;
            lonMin = (float)Math.Truncate(num2);
            num2 -= (double)lonMin;
            num2 *= 60.0;
            lonSec = (float)num2;
        }
    }
}
