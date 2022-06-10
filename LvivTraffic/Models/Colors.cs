using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LvivTraffic.Models
{
    public class Colors
    {
        public Dictionary<double, string> colors;
        public Colors()
        {
            colors = new Dictionary<double, string>
            {
                { 0.0,"green" },
                {0.1,"green" },
                {0.2,"green" },
                {0.3,"yellow" },
                {0.4,"yellow" },
                {0.5,"orange" },
                {0.6,"orange" },
                {0.7,"pink" },
                {0.8,"red" },
                {0.9,"red" },
                {1.0,"red" }
            };
        }
    }
}