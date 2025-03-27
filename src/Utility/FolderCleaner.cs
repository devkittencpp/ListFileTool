using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileCleaner
{
    public class FolderCleaner
    {
        private readonly HashSet<string> allowedFiles;
        private readonly string rootDirectory;
        private int removedCount;

        public FolderCleaner(string rootDirectory, string listFilePath)
        {
            // Convert root directory to full path and ensure it ends with a directory separator.
            this.rootDirectory = Path.GetFullPath(rootDirectory);
            if (!this.rootDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                this.rootDirectory += Path.DirectorySeparatorChar;

            // Read and normalize allowed file paths from the list file.
            allowedFiles = new HashSet<string>(
                File.ReadAllLines(listFilePath)
                    .Select(line => NormalizePath(line)),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Deletes files that are in the allowed list and then deletes empty folders recursively.
        /// Returns the number of files deleted.
        /// </summary>
        public int RemoveFilesInList()
        {
            removedCount = 0;
            ProcessDirectory(rootDirectory, isRoot: true);
            return removedCount;
        }

        /// <summary>
        /// Recursively processes a directory:
        /// - Deletes files that are allowed (in the allowed list).
        /// - Recursively processes subdirectories.
        /// - Deletes a subdirectory if it ends up empty.
        /// Returns true if the directory is empty (and eligible for deletion if not the root).
        /// </summary>
        private bool ProcessDirectory(string directory, bool isRoot = false)
        {
            // Process and potentially delete files that are in the allowed list.
            foreach (string file in Directory.GetFiles(directory))
            {
                // Compute the relative path from the root and normalize it.
                string relativePath = file.Substring(rootDirectory.Length);
                relativePath = NormalizePath(relativePath);

                // Instead of checking if the file is NOT in the allowed list,
                // we now delete it only if it IS in the allowed list.
                if (allowedFiles.Contains(relativePath))
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine("Deleted file: " + file);
                        removedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete file {file}: {ex.Message}");
                    }
                }
            }

            // Process each subdirectory recursively.
            foreach (string subDir in Directory.GetDirectories(directory))
            {
                // Recursively process the subdirectory.
                bool isEmpty = ProcessDirectory(subDir, isRoot: false);
                if (isEmpty)
                {
                    try
                    {
                        Directory.Delete(subDir);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete folder {subDir}: {ex.Message}");
                    }
                }
            }

            // Check if the current directory is empty (no files and no directories).
            bool directoryIsEmpty = !Directory.GetFiles(directory).Any() &&
                                    !Directory.GetDirectories(directory).Any();

            // Never remove the root directory, even if it is empty.
            return isRoot ? false : directoryIsEmpty;
        }

        /// <summary>
        /// Normalizes a path string by trimming whitespace, converting to lowercase,
        /// and replacing backslashes with forward slashes.
        /// </summary>
        private string NormalizePath(string path)
        {
            return path.Trim().ToLower().Replace('\\', '/').Replace("//", "/");
        }
    }
}
