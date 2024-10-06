using K4os.Compression.LZ4.Streams;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using UmaMusumeToolbox.DataDownload.Models;
using UmaMusumeToolbox.DataDownload.Utility;

namespace UmaMusumeToolbox.DataDownload
{
    public class Program
    {
        private static string _userSavedData = "";
        private static Settings _settings = new();

        public static char DirectorySeparator { get; private set; } = '/';

        static void Main()
        {
            try
            {
                // Build a config object
                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

                _settings = config.GetRequiredSection("Settings").Get<Settings>();
                RetrieveResource resource = new(_settings);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    DirectorySeparator = '\\';
                    _userSavedData = GetPlatformFriendlyFilepath(_settings.UserSavedLocation);
                }

                string connectionString = GetPlatformFriendlyFilepath(_settings.MetaDbFilepath);
                List<BlobInfo> blobs = SqliteUtility.GetAllBlobInfo(connectionString);
                List<Task> downloadTasks = [];
                int numFiles = 0;

                Console.WriteLine("Starting download...");
                if (_settings.SkipExistingFiles)
                {
                    Console.WriteLine("Skip existing files...");
                }

                // Download meta info
                foreach (BlobInfo blob in blobs)
                {
                    // Create root directory folders
                    if (blob.BlobPath.StartsWith("//"))
                    {
                        Directory.CreateDirectory(_settings.UserSavedLocation + DirectorySeparator + blob.BlobPath.Replace("//", ""));
                        continue;
                    }

                    string destinationFilepath = GetPhysicalFilepath(blob.BlobPath);

                    if (string.IsNullOrEmpty(destinationFilepath)
                        || (_settings.SkipExistingFiles && File.Exists(destinationFilepath)))
                    {
                        numFiles++;
                        continue;
                    }
                    
                    // Queue up the download
                    downloadTasks.Add(resource.DownloadFileAsync(blob.Hash, blob.Type, destinationFilepath));
                    numFiles++;

                    // Limit to 200 concurrent downloads and wait until they're all done
                    if (downloadTasks.Count >= 200)
                    {
                        Task.WaitAll(downloadTasks.ToArray());
                        downloadTasks.Clear(); // prep for next batch
                        Console.WriteLine($"Completed {numFiles} of {blobs.Count} files");
                    }
                }

                Task.WaitAll(downloadTasks.ToArray());

                List<string> lz4Files = blobs
                    .Where(name => name.BlobPath.EndsWith(".lz4"))
                    .Select(blob => blob.BlobPath)
                    .ToList();

                // Decode any LZ4 files
                foreach (string lz4File in lz4Files)
                {
                    string filename = GetPlatformFriendlyFilepath(_userSavedData + DirectorySeparator + lz4File);
                    int lastIndex = lz4File.LastIndexOf(DirectorySeparator);

                    // Trim any path info
                    if (lastIndex > 0)
                    {
                        filename = filename.Substring(lastIndex + 1);
                    }

                    using (LZ4DecoderStream source = LZ4Stream.Decode(File.OpenRead(filename)))
                    {
                        filename = filename.Replace(".lz4", "");
                        using (FileStream target = File.Create(filename))
                        {
                            source.CopyTo(target);
                            Console.WriteLine($"\n{lz4File} has been decoded!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                CloseProgram(1);
            }

            Console.WriteLine("\nYay! We're done downloading! Check for any errors above.");
            CloseProgram();
        }

        #region Private Methods
        /// <summary>
        /// Create the destination filepath for the <paramref name="blobText"/>'s file
        /// </summary>
        /// <param name="blobText">The internal filepath for the BLOB</param>
        /// <returns>The destination filepath</returns>
        private static string GetPhysicalFilepath(string blobText)
        {
            try
            {
                string blobFilepath = GetPlatformFriendlyFilepath(blobText);
                int index = blobFilepath.LastIndexOf(DirectorySeparator);

                // Check if file is in root directory
                if (index == -1)
                {
                    return $"{_userSavedData}{DirectorySeparator}{blobFilepath}";
                }

                string blobTextFilepath = blobFilepath.Substring(0, index);
                string parsedFilename = blobFilepath.Substring(index + 1, blobFilepath.Length - index - 1);

                string folderPath = $"{_userSavedData}{DirectorySeparator}{blobTextFilepath}";
                string entireFilepath = $"{folderPath}{DirectorySeparator}{parsedFilename}";

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                return entireFilepath;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        /// <summary>
        /// Make sure the file directory separator "/" or "\" is friendly to the current OS
        /// </summary>
        /// <param name="filepath">Filepath being checked for traversing</param>
        /// <returns></returns>
        private static string GetPlatformFriendlyFilepath(string filepath)
        {
            return filepath.Replace('/', DirectorySeparator);
        }

        /// <summary>
        /// Depending on the OS, close the program gracefully
        /// </summary>
        /// <param name="exitCode">0 means successful exit; otherwise failed exit</param>
        private static void CloseProgram(int exitCode = 0)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("\nPress any key to close this program...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("\nProgram finished executing.");
                Console.WriteLine("Exiting now...");
            }

            Environment.Exit(exitCode);
        }
        #endregion
    }
}
