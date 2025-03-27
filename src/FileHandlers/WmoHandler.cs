using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ListFileTool.FileHandlers
{
  class WmoHandler : FileHandler
  {
    public WmoHandler(string f) : base(f)
    {

    }

    public override void GenerateListfile()
    {
      base.GenerateListfile();

      if(!File.Exists(file))
      {
        return;
      }

      if(name != "")
      {
        string wmo = name.Remove(name.Length - 4) + "_"; var data = File.ReadAllBytes(file);

        int nGroups = BitConverter.ToInt32(data, 0x18);

        for (int i = 0; i < nGroups; i++)
        {
          AddFile(wmo + (i < 10 ? "0" : "") + (i < 100 ? "0" : "") + i + ".wmo");
        }
      }
      
      // todo : check for other stuff
    }
  }
}
