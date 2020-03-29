using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GEOMiner.Classes
{
    public class File
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public int Guete { get; set; }
        public Content content { get; set; }

        public File() { }

        public string ExceptionMessage { get; set; }
        public int? ID { get; set; }
    }
}
