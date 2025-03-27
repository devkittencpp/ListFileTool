using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ListFileTool.FileHandlers
{
  class M2Handler : FileHandler
  {
    byte[] data;

    public M2Handler(string f) : base(f)
    {
    }

    public override void GenerateListfile()
    {
      if (name.Length <= 4)
      {
        return;
      }

      AddSkins();
      AddFile(name.Remove(name.Length - 3) + ".phys");
      AddFile(name);

      if(!File.Exists(file))
      {
        return;
      }

      data = File.ReadAllBytes(file);

      int md20 = LocateMD20Chunk();
      if(md20 < 0)
      {
        // todo: error / exception ?
        return;
      }

      int ntex = ReadInt(md20 + 0x50);
      int ofs_tex = md20 + ReadInt(md20 + 0x54);
      
      for(int i=0; i<ntex; ++i)
      {
        int pos = ofs_tex + i * 0x10;

        // only type 0 have filenames
        if(ReadInt(pos) != 0)
        {
          continue;
        }

        AddFile(ReadStr(md20 + ReadInt(pos + 0xC), ReadInt(pos + 0x8) - 1));
      }      
    }

    private void AddSkins()
    {
      string skin = name.Remove(name.Length - 3);
      for (int i = 0; i < 4; i++)
        AddFile(skin + "0" + i + ".skin");
    }

    private int LocateMD20Chunk()
    {
      for (int i = 0; i < data.Length - 3; i++)
        if (data[i] == 0x4D && data[i + 1] == 0x44 && data[i + 2] == 0x32 && data[i + 3] == 0x30)
          return i;
      // todo : add error log
      return -1;
    }

    private string ReadStr(int pos, int length)
    {
      byte[] temp = new byte[length];
      Buffer.BlockCopy(data, pos, temp, 0, length);
      return Encoding.ASCII.GetString(temp).ToLower();
    }

    private int ReadInt(int pos)
    {
      return BitConverter.ToInt32(data, pos);
    }

    // ToDo : search anim files
    // ToDo : guess skin texture name for creature and item ?
  }
}
