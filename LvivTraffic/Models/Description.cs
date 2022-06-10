using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LvivTraffic.Models
{
    public class Description
    {
        public Dictionary<double, string> descriptions;
        public Description()
        {
            descriptions = new Dictionary<double, string>
            {
                { 0.0,"Дорога вільна!" },
                {0.1,"Дорога вільна!" },
                {0.2,"Дорога вільна!" },
                {0.3,"Місцями затруднення!" },
                {0.4,"Місцями затруднення!" },
                {0.5,"Рух щільний!" },
                {0.6,"Рух щільний!" },
                {0.7,"Рух затруднено!" },
                {0.8,"Серйозні пробки!" },
                {0.9,"Серйозні пробки!" },
                {1.0,"Багатокілометрові затори!" }
            };
        }
    }
}