using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LvivTraffic.Models
{
    public class Marker
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CarCount { get; set; }
        public double Congestion { get; set; }
        public double Percent { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string Message { get; set; }
        public string Street { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public byte[] Image { get; set; }
    }
}