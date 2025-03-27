using System.Collections.Generic;

namespace ListFileTool
{
    public static class GlobalStorage
    {
        public static string InputFolderPath { get; set; } = string.Empty;
        public static string DataFolderPath { get; set; } = string.Empty;
        public static string OutputFolderPath { get; set; } = string.Empty;
        public static string ListFilePath { get; set; } = string.Empty;
        public static string ClientListFileClean { get; set; } = string.Empty;

        public static List<string> FilePaths { get; set; } = new List<string>();
    }
}
