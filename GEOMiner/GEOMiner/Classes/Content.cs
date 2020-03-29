using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GEOMiner.Classes
{
    public class Content
    {
        public string Title { get; set; }
        public string geneName { get; set; }
        public string geneDesc { get; set; }
        public int ID { get; set; }
        public bool download { get; set; }
        public string ftplink { get; set; }
        public string accession { get; set; }
        public string Summary { get; set; }

        public string Type { get; set; }


        public Content(string _title, string _gname, string _gdesc, int _id, string _ftplink)
        {
            this.ID = _id;
            this.Title = _title;
            this.geneName = _gname;
            this.geneDesc = _gdesc;
            this.download = false;
            this.ftplink = _ftplink;
        }

        public Content(string _title, string _gname, string _gdesc, string _id, string _ftplink)
        {
            int c;
            Int32.TryParse(_id, out c);
            this.ID = c;

            this.Title = _title;
            this.geneName = _gname;
            this.geneDesc = _gdesc;
            this.download = false;
            this.ftplink = _ftplink;
        }

        public Content() { }

    }
}
