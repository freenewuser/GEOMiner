using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GEOMiner.Classes
{
    public class FList
    {
        private int maxId;
        public List<Filter> flist { get; set; }

        public FList() 
        {
            maxId = 1;
            flist = new List<Filter>();
        }

        public void Add(Filter filter)
        {
            filter.id = maxId;
            maxId++;
            flist.Add(filter);
            try
            {
                CheckFlist();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
            

        public void Remove(string _id)
        {
            for(var i=0;i<flist.Count;i++)
            {
                if (flist[i].id.ToString() == _id)
                    flist.Remove(flist[i]);
            }
            try
            {
                CheckFlist();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public FList(FList old)
        {
            this.maxId = 1;
            this.flist = new List<Filter>();

            for(var i=0;i<old.flist.Count;i++)
            {
                this.Add(old.flist[i]);
            }

            
        }

        private void CheckFlist()
        {
            Program.indexModel.ErrorFilterList = null;
            for (int i = 0; i < flist.Count; i++)
            {
                if (Program.indexModel.database.SingeFilters.Contains(flist[i].name) && flist.Count > 1)
                {
                    var msg = Controllers.HelperController.GetMessage("Helper.Err.DoubleSingleFilter");
                    Program.indexModel.ErrorFilterList = String.Format(msg.txt, flist[i].name);
                    break;
                }

                if (Program.indexModel.database.FilterDependencies.Count == 0)
                    continue;
                Tuple<string, string> dependency = Program.indexModel.database.FilterDependencies.Where(c => c.Item1 == flist[i].name).FirstOrDefault();
                if(dependency != null && !flist.Any( c=> c.name == dependency.Item2))
                {
                    var msg = Controllers.HelperController.GetMessage("Helper.Err.MissingDepedencyFilter");
                    Program.indexModel.ErrorFilterList = String.Format(msg.txt, flist[i].name, dependency.Item2);
                    break;
                }
            }
        }
    }
}
