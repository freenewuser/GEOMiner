using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace GEOMiner.Models
{
    public class IndexModel
    {
        public Classes.FList FilterList { get; set; }
        public string FilterName { get; set; }

        public List<string> SomeFilters { get; set; }

        public List<Classes.Content> ContentList { get; set; }

        public List<Classes.Content> PartialContentList { get; set; }

        public int cps { get; set; }
        public int actSite { get; set; }
        public int newSite { get; set; }

        public List<Classes.Database> DatabaseList { get; set; }

        public Classes.Database database { get; set; }

        /*public Dictionary<string, string> dataBases { get; set; }*/

        public string ExceptionMessage { get; set; }
        public string ErrorFilterList { get; set; }

        public void Init()
        {
            //this.FilterList = new Classes.FList();
            //this.SomeFilters = new List<string>();

            /*this.SomeFilters.Add("Attribute");
            this.SomeFilters.Add("Attribute Name");
            this.SomeFilters.Add("Author");
            this.SomeFilters.Add("Dataset Type");
            this.SomeFilters.Add("Description");
            this.SomeFilters.Add("Entry Type");
            this.SomeFilters.Add("Filter");
            this.SomeFilters.Add("Geo Accession");
            this.SomeFilters.Add("MeSH Terms");
            this.SomeFilters.Add("Number of Platform Probes");
            this.SomeFilters.Add("Number of Samples");
            this.SomeFilters.Add("Organism");
            this.SomeFilters.Add("Platform Technology Type");
            this.SomeFilters.Add("Project");
            this.SomeFilters.Add("Properties");
            this.SomeFilters.Add("Publication Date");
            this.SomeFilters.Add("Related Platform");
            this.SomeFilters.Add("Reporter Identifier");
            this.SomeFilters.Add("Sample Source");
            this.SomeFilters.Add("Sample Type");
            this.SomeFilters.Add("Sample Value Type");
            this.SomeFilters.Add("Submitter Institue");
            this.SomeFilters.Add("Subset Description");
            this.SomeFilters.Add("Subset Variable Type");
            this.SomeFilters.Add("Supplementary Files");
            this.SomeFilters.Add("Tag Length");
            this.SomeFilters.Add("Title");
            this.SomeFilters.Add("Update Date");
            this.SomeFilters.Add("Regular Expression");*/

            this.DatabaseList = new List<Classes.Database>();
            DirectoryInfo cfg = new DirectoryInfo(@"Config\Datenbanken");
            if(cfg.Exists)
            {
                foreach (FileInfo fi in cfg.GetFiles())
                {

                    if (fi.Extension != ".xml")
                        continue;

                    XmlDocument doc = new XmlDocument();
                    try
                    {
                        doc.Load(fi.FullName);
                    }
                    catch
                    {
                        Controllers.LogController.LogError($"Could not load file {fi.FullName}");
                    }

                    Classes.Database db = new Classes.Database();
                    XmlElement root = doc.DocumentElement;

                    if (!root.HasAttribute("key") | !root.HasAttribute("name"))
                    {
                        continue;
                    }
                    db.Name = root.GetAttribute("name");
                    db.KeyName = root.GetAttribute("key");

                    foreach (XmlNode node in root.SelectNodes("//filters//filter"))
                    {
                        if (db.Filter.Contains(node.InnerText) | node.InnerText == null)
                            continue;

                        db.Filter.Add(node.InnerText);

                        if (node.Attributes["single"] != null && node.Attributes["single"].Value == "J")
                        {
                            db.SingeFilters.Add(node.InnerText);
                        }
                        if (node.Attributes["dependency"] != null && node.Attributes["dependency"].Value != "")
                        {
                            db.FilterDependencies.Add(new Tuple<string, string>(node.InnerText, node.Attributes["dependency"].Value));
                        }
                    }

                    this.DatabaseList.Add(db);

                }
            }
            

            this.actSite = 0;
            this.cps = 10;
            this.newSite = -1;

            this.database = this.DatabaseList.FirstOrDefault();
            /*this.dataBases = new Dictionary<string, string>();
            this.dataBases.Add("GEO DataSets", "gds");
            this.dataBases.Add("GEO Profiles", "geoprofiles");

            this.database = dataBases["GEO DataSets"];*/

            var b = SetPartialList();

        }

        public bool SetPartialList()
        {
            PartialContentList = new List<Classes.Content>();
            if(ContentList != null)
            {
                for (int i = 0 + cps * actSite; i < ContentList.Count && i < cps * (actSite + 1); i++)
                {
                    PartialContentList.Add(ContentList[i]);
                }
            }
            
            return true;
        }
    }
}
