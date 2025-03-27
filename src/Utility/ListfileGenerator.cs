using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListFileGenerator
{
    class ListfileGenerator
    {
        private List<string> baseFiles = new List<string>();
        private HashSet<string> output = new HashSet<string>();

        public ListfileGenerator(string[] files)
        {
            baseFiles.AddRange(files);
        }

        public void Run()
        {
            
        }
    }
}

