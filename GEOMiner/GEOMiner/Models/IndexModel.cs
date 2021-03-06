﻿using System;
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
