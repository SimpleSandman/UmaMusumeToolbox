using UmaMusumeToolbox.DataDownload.Models;

namespace UmaMusumeToolbox.DataDownload
{
    public class RetrieveResource
    {
        private const string ASSETS_ENDPOINT = "Windows/assetbundles/";
        private const string GENERIC_ENDPOINT = "Generic/";
        private const string MANIFEST_ENDPOINT = "Manifest/";
        private readonly Settings _settings;
        private readonly HttpClient _httpClient = new HttpClient();

        public RetrieveResource(Settings settings)
        {
            _settings = settings;
            _httpClient.Timeout = TimeSpan.FromMinutes(settings.TimeoutFromMinutes);
        }

        /// <summary>
        /// Call the endpoint using the <paramref name="hash"/> and if the endpoint returns successfully, 
        /// download the file to the <paramref name="destinationFilepath"/>
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="type"></param>
        /// <param name="destinationFilepath"></param>
        /// <returns><see langword="true"/> if both the endpoint and download are successful; 
        /// otherwise <see langword="false"/></returns>
        public async Task<bool> DownloadFileAsync(string hash, string type, string destinationFilepath)
        {
            try
            {
                string url = BuildEndpointUrl(hash, type);
                if (string.IsNullOrEmpty(url))
                {
                    Console.WriteLine("\nDownload URL is empty");
                    return false;
                }

                return await HasFileDownloadedAsync(url, destinationFilepath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #region Private Methods
        /// <summary>
        /// Create the endpoint URL used to download a resource
        /// </summary>
        /// <param name="blobRowType">What type of BLOB is being downloaded</param>
        /// <param name="hash">The ID of the resource</param>
        /// <returns>Complete endpoint URL ready for download</returns>
        private string BuildEndpointUrl(string hash, string blobRowType)
        {
            try
            {
                string url;

                switch (blobRowType)
                {
                    case "master":
                    case "sound":
                    case "movie":
                    case "font":
                        url = _settings.HostName + GENERIC_ENDPOINT;
                        break;
                    default:
                        url = _settings.HostName + ASSETS_ENDPOINT;
                        break;
                }

                if (blobRowType.StartsWith("manifest"))
                {
                    url = _settings.HostName + MANIFEST_ENDPOINT;
                }

                return string.Concat(url, hash.AsSpan(0, 2), "/", hash);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        /// <summary>
        /// Attempt to download the file and see if it was successful
        /// </summary>
        /// <param name="url">The endpoint</param>
        /// <param name="destinationFilepath">The physical path to save the file</param>
        /// <returns>If the download was successful, return <see langword="true"></see>; otherwise <see langword="false"></see></returns>
        private async Task<bool> HasFileDownloadedAsync(string url, string destinationFilepath)
        {
            try
            {
                if (_settings.IsDebugMode)
                {
                    Console.WriteLine($"Started downloading to {destinationFilepath}");
                }

                byte[] data = await _httpClient.GetByteArrayAsync(url).ConfigureAwait(false);

                // Check for conflicting folder name
                if (Directory.Exists(destinationFilepath))
                {
                    int lastIndex = destinationFilepath.LastIndexOf(Program.DirectorySeparator);
                    destinationFilepath += Program.DirectorySeparator + 
                        destinationFilepath.Substring(lastIndex + 1);
                }

                await File.WriteAllBytesAsync(destinationFilepath, data).ConfigureAwait(false);

                if (_settings.IsDebugMode)
                {
                    Console.WriteLine($"Downloaded to {destinationFilepath}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFailed to download from \"{url}\"");
                Console.WriteLine($"Missing File: {destinationFilepath}");

                // Output the pre-designated message instead of the standard exception message
                if (ex.Message.Contains("The request was canceled due to the configured HttpClient.Timeout of"))
                {
                    Console.WriteLine("\n========== POSSIBLE WORKAROUNDS ==========\n");
                    Console.WriteLine($"The download for this file exceeded the {_settings.TimeoutFromMinutes} minute timeout. " +
                        "Please either restart the program or increase the timeout limit\n");
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }

                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException);

                    if (ex.InnerException.Message.Contains("The response ended prematurely.")
                        || ex.InnerException.Message.Contains("Unable to read data from the transport connection:"))
                    {
                        Console.WriteLine("\n========== POSSIBLE WORKAROUNDS ==========\n");
                        Console.WriteLine("NOTE: This could be due to a 403 (forbidden) error killing the connection/download too early.");
                        Console.WriteLine("- When this program has finished downloading, restart this program with \"SkipExistingFiles\": true in the \"appsettings.json\" config file " +
                            "so you can simply download the missing files that received this error.");
                        Console.WriteLine("- You can manually download this particular file by pasting the URL into your browser.");
                        Console.WriteLine("- You can also also validate this with an API platform like \"Postman\" with the URL posted above this message");
                    }
                }

                Console.WriteLine("\n==========================================\n");

                return false;
            }
        }
        #endregion
    }
}
