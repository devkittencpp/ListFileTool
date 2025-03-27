using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ListFileTool.FileHandlers
{
  class AdtHandler : FileHandler
  {
    enum chunks : int
    {
      MDDX = 1296909400,
      MWMO = 1297567055,
      MTEX = 1297368408,
    }

    byte[] data;

    public AdtHandler(string f) : base(f)
    {
      data = File.ReadAllBytes(f);
    }

    public override void GenerateListfile()
    {
      int pos = 0, magic = 0, size = 0;

      while(pos < data.Length)
      {
        magic = ReadInt(pos);
        size  = ReadInt(pos+4);

        switch(magic)
        {
          case ((int)chunks.MTEX): Read_MTEX(pos + 0x8, size); break;
          case ((int)chunks.MWMO): Read_MWMO(pos + 0x8, size); break;
          case ((int)chunks.MDDX): Read_MDDX(pos + 0x8, size); break;
        }

        pos += size + 0x8;
      }
    }

    private void Read_MDDX(int pos, int size)
    {
      int start = pos;
      while(pos < start + size)
      {
        string file = ReadStr(pos);
        pos += file.Length + 1;

        M2Handler mh = new M2Handler(file);
        mh.GenerateListfile();

        Listfile.UnionWith(mh.Listfile);
      }
    }

    private void Read_MWMO(int pos, int size)
    {
      int start = pos;
      while (pos < start + size)
      {
        string file = ReadStr(pos);
        pos += file.Length + 1;

        WmoHandler wh = new WmoHandler(file);
        wh.GenerateListfile();

        Listfile.UnionWith(wh.Listfile);
      }
    }

    private void Read_MTEX(int pos, int size)
    {
      int start = pos;
      while (pos < start + size)
      {
        string file = ReadStr(pos);
        pos += file.Length + 1;

        AddFile(file);
      }
    }

    private string ReadStr(int pos)
    {
      int end = pos;
      byte[] temp;

      // 0 = '\0' = string end
      while (data[end] != 0)
      {
        end++;
      }

      int size = end - pos;
      temp = new byte[size];

      Buffer.BlockCopy(data, pos, temp, 0, size);

      return Encoding.ASCII.GetString(temp).Replace('\0', '.').ToLower();
    }

    private int ReadInt(int pos)
    {
      return BitConverter.ToInt32(data, pos);
    }
  }
}
