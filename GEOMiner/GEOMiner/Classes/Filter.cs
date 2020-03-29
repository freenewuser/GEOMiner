using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GEOMiner.Classes
{
    public class Filter
    {
        public string name
        { get; set;
        }

        public string value { get; set; }

        public int id { get; set; }

        public Filter() { }

        public Filter(string _name, string _value)
        {
            name = _name;
            value = _value;

        }

        public string GetValueForQuery(string dbs)
        {
            switch (dbs)
            {
                case "geoprofiles":
                case "gds":  return GetValueForGeoProfiles();
            }
            return null;
        }

        public string GetValueForGeoProfiles()
        {
            String tmpValue = this.value;
            String ret = $"(";

            if (this.name == "Regular Expression")
                return this.value;

            while (tmpValue.Contains(","))
            {
                if (tmpValue.IndexOf(",") == 0)
                {
                    tmpValue = tmpValue.Substring(1);

                    continue;
                }
                ret = ret + $"{tmpValue.Substring(0, tmpValue.IndexOf(",")).TrimStart().TrimEnd().Replace(" ", "+")}[{this.name}]+OR+";

                tmpValue = tmpValue.Substring(tmpValue.IndexOf(",") + 1);
            }

            ret = ret + $"{tmpValue.TrimStart().TrimEnd().Replace(" ", "+")}[{this.name}])";

            return ret;
        }

        public string GetValueForGeoDataSets()
        {
            String tmpValue = this.value;
            String ret = $"(";

            while (tmpValue.Contains(","))
            {
                if (tmpValue.IndexOf(",") == 0)
                {
                    tmpValue = tmpValue.Substring(1);

                    continue;
                }
                ret = ret + $"{tmpValue.Substring(0, tmpValue.IndexOf(",")).TrimStart().TrimEnd().Replace(" ", "+")}[{this.name}]+OR+";

                tmpValue = tmpValue.Substring(tmpValue.IndexOf(",") + 1);
            }

            ret = ret + $"{tmpValue.TrimStart().TrimEnd().Replace(" ", "+")}[{this.name}])";

            return ret;
        }
    }
}
