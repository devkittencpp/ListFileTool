using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ListFileTool.FileHandlers
{
    public class FileHandler
    {
        protected string file;
        protected string name = "";
        public HashSet<string> Listfile { get; private set; } = new HashSet<string>();

        public FileHandler(string f)
        {
            file = f;
            name = GuessName();
        }

        public virtual void GenerateListfile()
        {
            if (!File.Exists(file))
            {
                AddFile(file);
                return;
            }

            string exp = @"\.((([a-zA-Z0-9_\- ]|([0-9]\.[0-9]))+)(\\|\/))+([a-zA-Z0-9_\- ])+\.(blp|mdx|m2|wmo|adt)";
            Regex regex = new Regex(exp);
            MatchCollection mc = regex.Matches(HextoAscii(file));
            AddFile(name);

            foreach (Match m in mc)
            {
                string s = m.ToString().Substring(1);
                if (s.EndsWith("mdx", StringComparison.CurrentCultureIgnoreCase))
                    s = s.Replace(".mdx", ".m2");

                AddFile(s);
            }
        }

        protected string HextoAscii(string filename)
        {
            byte[] data = File.ReadAllBytes(filename);
            string str = Encoding.ASCII.GetString(data);
            str = str.Replace('\0', '.');
            return str.ToLower();
        }

        protected void AddFile(string f)
        {
            if (!Listfile.Contains(f))
            {
                Listfile.Add(f);
                if (f.EndsWith(".blp", StringComparison.CurrentCultureIgnoreCase))
                    AddBLP(f.ToLower());
            }
        }

        private void AddBLP(string blp)
        {
            // Process only if the file relates to tilesets.
            if (!blp.Contains("tileset", StringComparison.OrdinalIgnoreCase))
                return;

            if (blp.EndsWith("_h.blp", StringComparison.CurrentCultureIgnoreCase))
            {
                Listfile.Add(blp.Remove(blp.Length - 6, 2));
                Listfile.Add(blp.Replace("_h", "_s"));
            }
            else if (blp.EndsWith("_s.blp", StringComparison.CurrentCultureIgnoreCase))
            {
                Listfile.Add(blp.Remove(blp.Length - 6, 2));
                Listfile.Add(blp.Replace("_s", "_h"));
            }
            else
            {
                Listfile.Add(blp.Insert(blp.Length - 4, "_h"));
                Listfile.Add(blp.Insert(blp.Length - 4, "_s"));
            }
        }

        private string GuessName()
        {
            // Attempt to guess the root folder name from the file path.
            string start = "(Cameras|Creature|DBFilesClient|Dungeons|Interface|Item|QA_TOOLSTEST|SCREENEFFECTS|Shaders|Sound|Spell|" +
                           "Spells|Textures|Tileset|World|environments|fonts|interiors|particles|spell|test|trialllists|users|xTexture)";
            string n = Regex.Match(file, start + @"(\\|\/).*", RegexOptions.IgnoreCase).Value;
            return n;
        }
    }
}
