using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;



namespace GEOMiner.Controllers
{
    public class queryController : Controller
    {
        private string WebEnvironment;
        private IEnumerable<XElement> summaries;
        private IEnumerable<String> IDs;
        private Classes.ESearch eSearch;
        private Classes.ESummary eSummary;
        private static DateTime StartZeit;

        public List<Classes.Content> GetContent(List<Classes.Filter> flist)
        {
            LogController.Begin();
            StartZeit = DateTime.Now;
            eSearch = new Classes.ESearch(Program.indexModel.database.KeyName);
            eSummary = new Classes.ESummary(Program.indexModel.database.KeyName);

            try
            {
                PassQuery(flist, eSearch);
            }                              // fills Webenvironment and Ids
            catch
            {
                Controllers.LogController.LogError($"Query via Flist failed with {flist.Count()} elements");
                throw;
            };

            try
            {
                PassWebEnv(this.WebEnvironment, eSummary);
            }    // fills titles and summaries

            catch(Exception ex)
            {
                Controllers.LogController.LogError($"Query via WebENv failed with {this.WebEnvironment} for utility 'esummary', ERROR: {ex.ToString()}");
                throw;
            };

            LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
            return GetContent();
        }

        //#################################################################################################
        public void PassQuery(List<GEOMiner.Classes.Filter> inputs, Classes.EUtilities u)
        {
            LogController.Begin();
            StartZeit = DateTime.Now;

            string query = "&term=";
            List<String> existFilters = new List<String>();
            foreach (GEOMiner.Classes.Filter input in inputs)
            {

                if (existFilters.Contains(input.name))
                    continue;

                if( query != "&term=")
                {
                    query = query + "+AND+";
                }

                Classes.Filter tmp = input;

                foreach( GEOMiner.Classes.Filter i in inputs)
                {
                    if(i.id != input.id && i.name == input.name)
                    {
                        tmp.value = tmp.value + "," + i.value;
                    }
                }

                query = query + tmp.GetValueForQuery(Program.indexModel.database.KeyName);

                existFilters.Add(input.name);
            }

            query = query.Trim().Replace(' ', '+');

            query = FormulateRequest(query, u.EType, Program.indexModel.database.KeyName);

            try 
            {
                (this.WebEnvironment, this.IDs) = u.SendRequest(query); 
            }
            catch(Exception ex)
            { 
                Controllers.LogController.LogError($"Query failed with {query} for utility '{u.EType}' , ERROR: {ex.ToString()} "); 
            };

            LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));

        }

        //#################################################################################################
        public void PassWebEnv(String keys, Classes.EUtilities u)
        {
            LogController.Begin();
            StartZeit = DateTime.Now;
            // WebEnv keys best be stored as fully realised request Strings as soon as they get parsed
            String query = this.FormulateRequest(keys, u.EType, Program.indexModel.database.KeyName);
            try
            {
                this.summaries = u.SendRequest_(query);
            }
            catch
            {
                Controllers.LogController.LogError($"Query failed with {query} for utility '{u.EType}' ");
            };

            LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));

        }


        //#################################################################################################
        public String FormulateRequest(String request, String request_type = "esearch", String database = "geoprofiles", String version = "&version=2.0")
        {
            LogController.Begin();
            StartZeit = DateTime.Now;

            String base_address = $"https://eutils.ncbi.nlm.nih.gov/entrez/eutils/{request_type}.fcgi?db={database}{version}&retmax=1000";
            String authenticators = "&tools=GEOMiner&Email=GEOMiner@ovgu.de";

            String url = base_address + request + authenticators;
            if (this.WebEnvironment == null)
                url = url + "&usehistory=y";

            Controllers.LogController.LogMessage($"Parsed url: {url}");

            LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
            return url;
        }

        //#################################################################################################
        public List<Classes.Content> GetContent()
        {
            LogController.Begin();
            StartZeit = DateTime.Now;
            List<Classes.Content> ret = new List<Classes.Content>();

            if (summaries == null)
                return ret;

            var s = summaries.GetEnumerator();

            while (s.MoveNext())
            {
                DateTime start = DateTime.Now;
                var el = s.Current;
                var newContent = new Classes.Content();
                int c;
                Int32.TryParse(el.Attribute("uid").Value, out c);
                newContent.ID = c;

                using (XmlReader reader = el.CreateReader())
                {
                    while(reader.Read())
                    {
                        switch(reader.NodeType, reader.Name, Program.indexModel.database.KeyName)
                        {
                            case (XmlNodeType.Element, "title", "geoprofiles"):
                            case (XmlNodeType.Element, "title", "gds"):
                                XElement element = XElement.ReadFrom(reader) as XElement;
                                if(element != null)
                                {
                                    newContent.Title = element.Value;
                                }
                                break;

                            case (XmlNodeType.Element, "geneName", "geoprofiles"):
                            case (XmlNodeType.Element, "geneName", "gds"):
                                element = XElement.ReadFrom(reader) as XElement;
                                if (element != null)
                                {
                                    newContent.geneName = element.Value;
                                }
                                break;

                            case (XmlNodeType.Element, "geneDesc", "geoprofiles"):
                            case (XmlNodeType.Element, "geneDesc", "gds"):
                                element = XElement.ReadFrom(reader) as XElement;
                                if (element != null)
                                {
                                    newContent.geneDesc = element.Value;
                                }
                                break;

                            case (XmlNodeType.Element, "FTPLink", "gds"):
                                element = XElement.ReadFrom(reader) as XElement;
                                if (element != null)
                                {
                                    newContent.ftplink = element.Value;
                                }
                                break;
                            case (XmlNodeType.Element, "Accession", "gds"):
                                element = XElement.ReadFrom(reader) as XElement;
                                if (element != null)
                                {
                                    newContent.accession = element.Value;
                                }
                                break;
                            case (XmlNodeType.Element, "GDS", "geoprofiles"):
                                element = XElement.ReadFrom(reader) as XElement;
                                if (element != null)
                                {
                                    newContent.accession = "GDS"+element.Value;
                                }
                                break;
                            case (XmlNodeType.Element, "summary", "gds"):
                                element = XElement.ReadFrom(reader) as XElement;
                                if (element != null)
                                {
                                    newContent.Summary = element.Value;
                                }
                                break;
                            case (XmlNodeType.Element, "Samples", "gds"):
                                element = XElement.ReadFrom(reader) as XElement;
                                break;
                            case (XmlNodeType.Element, "gdsType", "gds"):
                                element = XElement.ReadFrom(reader) as XElement;
                                if( element != null)
                                {
                                    newContent.Type = element.Value;
                                }
                                break;

                        }
                    }
                }

                ret.Add(newContent);
                LogController.LogMessage(String.Format("Zeit pro Summary: {0}", DateTime.Now - start));
            }

            LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
            return ret;
        }

        
    }
}