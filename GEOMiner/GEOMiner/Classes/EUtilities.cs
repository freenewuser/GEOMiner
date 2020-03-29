using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace GEOMiner.Classes
{
    public class EUtilities
    {
        public string EType = "eutilitie";
        private static string database;
        public static class databases { public const string GEODatasets = "gds", Geoprofiles = "geoprofiles"; };

        public EUtilities(string dbs) 
        { 
            database = dbs; 
        }

        public virtual IEnumerable<XElement> SendRequest_(string s)
        {
            return null;
        }

        public virtual (String, IEnumerable<String>) SendRequest(string s)
        {
            return (null, null);
        }

        public virtual void CheckResponse(IEnumerable<XElement> response)
        {
            if (response.ElementAt(0).Name == "Error")
            {
                Controllers.LogController.LogError($"Ill-formatted request, Response:\n {response.ElementAt(0).Value}");
                throw new Exception("Ill-formatted response");
            };

        }

        public virtual void CheckResponse(IEnumerable<Tuple<XElement, XElement>> response)
        {
            if (response.ElementAt(0).Item1.Name == "Error")
            {
                Controllers.LogController.LogError($"Ill-formatted request, Response:\n {response.ElementAt(0).Item1.Value}");
                throw new Exception("Ill-formatted response");
            };

        }
    }
}