using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GEOMiner.Classes
{
    public class ESummary : EUtilities
    {

        private static string database;
        public ESummary(string dbs) : base(dbs)
        {
            this.EType = "esummary";
            database = dbs;
        }

        private static DateTime StartZeit;

        public override IEnumerable<XElement> SendRequest_(String uri)
        {
            Controllers.LogController.Begin();
            StartZeit = DateTime.Now;

            IEnumerable<XElement> DocSummaries = SkimXML(uri);

            CheckResponse(DocSummaries);

            Controllers.LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
            return DocSummaries;
        }

        static List<XElement> SkimXML(String URI)
        {
            Controllers.LogController.Begin();
            StartZeit = DateTime.Now;

            List<XElement> ret = new List<XElement>();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = System.Xml.DtdProcessing.Parse;
            using (XmlReader reader = XmlReader.Create(URI, settings)) // directly reads the https connection
            {
                reader.MoveToContent();
                // Parse the file and display each of the nodes.  
                while (reader.Read())
                {
                    switch (reader.NodeType, reader.Name, true)
                    {
                        case (XmlNodeType.Element, "ERROR", true):
                            XElement el = XElement.ReadFrom(reader) as XElement;
                            if (el != null) 
                                throw new Exception($"Ill-formatted request, Response:\n {el.Value}");
                            else 
                                throw new Exception($"Ill-formatted response");

                        case (XmlNodeType.Element, "DocumentSummary", true):
                            el = XElement.ReadFrom(reader) as XElement;
                            if (el != null)
                            {
                                ret.Add(el);
                            }
                            //searchterm = "summary";
                            break;

                    }
                }
            }

            Controllers.LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
            //10.000 
            return ret;
        }
    }
}
