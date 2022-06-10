using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LvivTraffic.Models
{
    public class Camera
    {
        public int Id { get; set; }
        public string DirectoryName { get; set; }
        public string Name { get; set; }
        //public string CarCount { get; set; }
        public bool IsAdded { get; set; }
        //public int Px1 { get; set; }
        //public int Px2 { get; set; }
        //public int Py1 { get; set; }
        //public int Py2 { get; set; }
        public int MarkerId { get; set; }
    }
}