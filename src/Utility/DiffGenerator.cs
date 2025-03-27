using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ListFileTool
{
    class DiffGenerator
    {
        public string Listfile { get; set; }
        public string Folder { get; set; }

        public DiffGenerator(string listfile, string directory)
        {
            Listfile = listfile;
            Folder = directory;
        }

        public void GenerateDiff(string output = "diff.txt")
        {
            HashSet<string> extracted = LoadExtracted(Folder, Folder);
            HashSet<string> listfile = new HashSet<string>();
            List<string> diff = new List<string>();

            foreach (string s in File.ReadAllLines(Listfile))
                listfile.Add(s.ToLower());

            foreach (string s in listfile)
                if (!extracted.Contains(s))
                    diff.Add(s);

            using (StreamWriter sw = new StreamWriter("./diff.txt", false, Encoding.UTF8))
            {
                foreach(string f in diff)
                {
                    sw.WriteLine(f);
                    sw.Flush();
                }                 
            }
        }

        private HashSet<string> LoadExtracted(string path, string root)
        {
            int rootSize = root.Length+(root.EndsWith("\\")  || root.EndsWith("/") ? 0 : 1);
            HashSet<string> extracted = new HashSet<string>();

            foreach(string file in Directory.GetFiles(path))
            {
                string f = file.Substring(rootSize).ToLower();
                extracted.Add(f);
            }

            foreach (string dir in Directory.GetDirectories(path))
                foreach (string f in LoadExtracted(dir, root))
                    extracted.Add(f);
                

            return extracted;
        }
    }
}
