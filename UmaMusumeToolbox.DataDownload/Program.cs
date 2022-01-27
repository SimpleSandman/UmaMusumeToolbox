using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;

using UmaMusumeToolbox.DataDownload.Models;

namespace UmaMusumeToolbox.DataDownload
{
    public class Program
    {
        private static string _userSavedData = "";
        private static Settings _settings = new Settings();

        public static char DirectorySeparator { get; private set; } = '/';

        static void Main(string[] args)
        {
            try
            {
                // Build a config object, using env vars and JSON providers.
                IConfiguration config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build();

                _settings = config.GetRequiredSection("Settings").Get<Settings>();
                RetrieveResource resource = new RetrieveResource(_settings);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    DirectorySeparator = '\\';
                    _userSavedData = GetPlatformFriendlyFilepath(_settings.UserSavedLocation);
                }

                string connectionString = GetPlatformFriendlyFilepath(_settings.MetaDbFilepath);
                List<BlobInfo> blobs = SqliteUtility.GetAllBlobInfo(connectionString);
                List<Task> tasks = new List<Task>();
                int numFiles = 0;

                Console.WriteLine("Starting download...");
                if (_settings.SkipExistingFiles)
                {
                    Console.WriteLine("Skip existing files...");
                }

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
                    
                    tasks.Add(resource.DownloadFileAsync(blob.Hash, blob.Type, destinationFilepath));
                    numFiles++;

                    // Limit to 200 concurrent downloads and wait until they're all done
                    if (tasks.Count >= 200)
                    {
                        Task.WaitAll(tasks.ToArray());
                        tasks.Clear(); // prep for next batch
                        Console.WriteLine($"Completed {numFiles} of {blobs.Count} files");
                    }
                }

                Task.WaitAll(tasks.ToArray());

                //using (var source = LZ4Stream.Decode(File.OpenRead(filename + ".lz4")))
                //using (var target = File.Create(filename))
                //{
                //    source.CopyTo(target);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                CloseProgram(1);
            }

            Console.WriteLine("\nYay! We're done downloading! Check any errors above.");
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
