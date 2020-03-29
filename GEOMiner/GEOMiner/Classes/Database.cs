using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GEOMiner.Classes
{
    public class Database
    {
        public string Name { get; set; }
        public string KeyName { get; set; }
        public List<string> Filter { get; set; }
        public List<Tuple<string, string>> FilterDependencies { get; set; }
        public List<string> SingeFilters { get; set; }

        public Database()
        {
            this.Filter = new List<string>();
            this.FilterDependencies = new List<Tuple<string, string>>();
            this.SingeFilters = new List<string>();
        }
    }
}
