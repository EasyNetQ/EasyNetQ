using System.IO;
using EasyNetQ.SagaHost.IO.Ntfs;

namespace EasyNetQ.SagaHost
{
    public class UrlZoneService
    {
        /// <summary>
        /// Clears the URLZONE from all files in the given directory.
        /// MEF ignores files with a zone other than local machine.
        /// http://mikehadlow.blogspot.com/2011/07/detecting-and-changing-files-internet.html
        /// </summary>
        /// <param name="directoryPath"></param>
        public static void ClearUrlZonesInDirectory(string directoryPath)
        {
            foreach (var filePath in Directory.EnumerateFiles(directoryPath))
            {
                var fileInfo = new FileInfo(filePath);
                fileInfo.DeleteAlternateDataStream("Zone.Identifier");
            }
        }
    }
}