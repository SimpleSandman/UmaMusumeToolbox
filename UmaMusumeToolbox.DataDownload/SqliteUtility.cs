using System.Data.SQLite;

using UmaMusumeToolbox.DataDownload.Models;

namespace UmaMusumeToolbox.DataDownload
{
    public static class SqliteUtility
    {
        /// <summary>
        /// From the "meta" database file, grab all of the BLOB info
        /// </summary>
        /// <param name="connectionString">The filepath to the "meta" file</param>
        /// <returns>List of BLOB info with their <see cref="BlobInfo.BlobPath"/>, 
        /// <see cref="BlobInfo.Hash"/>, and <see cref="BlobInfo.Type"/></returns>
        public static List<BlobInfo> GetAllBlobInfo(string connectionString)
        {
            List<BlobInfo> listBlobInfo = new List<BlobInfo>();

            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={connectionString}"))
            {
                connection.Open();

                using (SQLiteCommand tableNameCommand = connection.CreateCommand())
                {
                    tableNameCommand.CommandText = "select n, h, m from a";

                    using (SQLiteDataReader reader = tableNameCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listBlobInfo.Add(new BlobInfo
                            {
                                BlobPath = reader.GetString(0),
                                Hash = reader.GetString(1),
                                Type = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return listBlobInfo;
        }
    }
}
