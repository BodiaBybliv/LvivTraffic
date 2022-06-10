using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LvivTraffic.Models
{
    public class ProcessingCamera
    {
        public int Id { get; set; }
        public int CarCount { get; set; }
        public int Px1 { get; set; }
        public int Px2 { get; set; }
        public int Py1 { get; set; }
        public int Py2 { get; set; }
        public int CameraId { get; set; }
    }
}