using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListFileTool
{
    class FileExtractor
    {
        public HashSet<string> Listfile { get; private set; } = new HashSet<string>();
        private string file;

        public static void CopyFiles(string listfilePath, string sourceDirectory, string targetDirectory, Action<string> updateStatus)
        {
            // Check if the listfile exists
            if (!File.Exists(listfilePath))
            {
                throw new FileNotFoundException($"Listfile not found: {listfilePath}");
            }

            // Ensure the target directory exists
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Build a dictionary mapping lower-case relative paths to the actual source file paths.
            // This allows us to do case-insensitive matching.
            var fileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                // Get the file's relative path from the source directory.
                string relativePath = file.Substring(sourceDirectory.Length)
                                          .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                fileMap[relativePath.ToLowerInvariant()] = file;
            }

            // Read all non-empty lines from the listfile.
            string[] filePaths = File.ReadAllLines(listfilePath);
            int totalFiles = filePaths.Count(f => !string.IsNullOrWhiteSpace(f));
            int processedFiles = 0;

            foreach (string filePath in filePaths)
            {
                string trimmedPath = filePath.Trim().TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.IsNullOrEmpty(trimmedPath))
                {
                    continue;
                }

                // Convert the trimmed path to lowercase to perform a case-insensitive lookup.
                string lookupKey = trimmedPath.ToLowerInvariant();
                if (fileMap.TryGetValue(lookupKey, out string actualSourcePath))
                {
                    // Construct the target path using the original trimmed path.
                    string targetPath = Path.Combine(targetDirectory, trimmedPath);
                    string targetSubfolder = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetSubfolder))
                    {
                        Directory.CreateDirectory(targetSubfolder);
                    }

                    try
                    {
                        File.Copy(actualSourcePath, targetPath, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error copying {actualSourcePath}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Skipping invalid file path: {trimmedPath}");
                }

                processedFiles++;
                updateStatus?.Invoke($"Copied {processedFiles} of {totalFiles} files...");
            }
        }


        public FileExtractor(string f)
        {
            file = f;
        }

        private void GenerateListfile()
        {

        }
    }
}
