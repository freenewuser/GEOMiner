using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GEOMiner.Classes
{
    public class ESearch : EUtilities
    {
        private static string database;
        public ESearch(string dbs) : base (dbs)
        {
            this.EType = "esearch";
            database = dbs;
        }


        private static DateTime StartZeit;

        public override (String, IEnumerable<String>) SendRequest(String uri)
        {
            Controllers.LogController.Begin();
            StartZeit = DateTime.Now;
            // TODO: function sends two requests, which is not necessary

            (List<XElement> LWebEnv, List<XElement> LItems) = SkimXML(uri);

            String WebEnv = null;
            IEnumerable<String> IDList;
            if (LWebEnv.Count != 0)
            {
                WebEnv = $"&query_key={LWebEnv.ElementAt(0).Value}&WebEnv={LWebEnv.ElementAt(1).Value}";
            }
            
            IDList = from elem in LItems select elem.Value;


            Controllers.LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
            return (WebEnv, IDList);
        }


        static (List<XElement>, List<XElement>) SkimXML(String URI)
        {
            Controllers.LogController.Begin();
            StartZeit = DateTime.Now;

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = System.Xml.DtdProcessing.Parse;

            List<XElement> WebEnv = new List<XElement>();
            List<XElement> Items = new List<XElement>();

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

                        case (XmlNodeType.Element, "QueryKey", true):
                            // QueryKey Elements appear to miss a line separator such that
                            // you have to call ReadFrom twice in immediate succession
                            if (WebEnv.Count == 0)
                            {
                                el = XElement.ReadFrom(reader) as XElement;
                                if (el != null) 
                                    WebEnv.Add(el);

                                if (reader.Name == "WebEnv")
                                {
                                    el = XElement.ReadFrom(reader) as XElement;
                                    if (el != null)
                                        WebEnv.Add(el);
                                }
                            }
                            break;

                        case (XmlNodeType.Element, "Id", true):
                            //if (reader.Name == searchterm)
                            {
                                el = XElement.ReadFrom(reader) as XElement;
                                if (el != null) 
                                    Items.Add(el);
                            }
                            break;

                        case (XmlNodeType.Element, "TranslationSet", true):
                            reader.Close();
                            break;
                    }
                }
            }

            Controllers.LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
            return (WebEnv, Items);
        }
    }
}
