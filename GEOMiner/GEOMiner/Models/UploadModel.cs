using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace GEOMiner.Models
{
    public class UploadModel
    {
        public string fileName { get; set; }


        public IFormFile file { get; set; }

        public string fileString { get; set; }

        public string ExceptionMessage { get; set; }
        public List<Classes.File> FileList { get; set; }
        public List<string[][]> ArrayList { get; set; }
        public List<int> Offset { get; set; }
        public static readonly int TableWidth = 8;
        public static readonly int TableHeight = 4; //includes headers
        public List<List<int>> IntegerList { get; set; }
        //public List<Tuple<Classes.File, string[][], List<int>>> ColumnList { get; set; }

        public string ViewMode { get; set; }
        public List<int> SomeNumbers { get; set; }

        public int Step = 1;
    }
}
