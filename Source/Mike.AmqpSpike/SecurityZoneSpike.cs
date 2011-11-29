using System;
using System.IO;
using System.Security;
using System.Security.Policy;
using Mike.AmqpSpike.IO.Ntfs;

namespace Mike.AmqpSpike
{
    public class SecurityZoneSpike
    {
        //private const string path = @"C:\temp\ZoneTest.txt";
        private const string path = @"C:\Source\Mike.AmqpSpike\EasyNetQ.SagaHost\Sagas\EasyNetQ.Tests.SimpleSaga.dll";

        public void CanWeDetectTheSecurityZoneOnAFile()
        {
            var zone = Zone.CreateFromUrl("file:///C:/temp/ZoneTest.txt");
            if (zone.SecurityZone != SecurityZone.MyComputer)
            {
                Console.WriteLine("File is blocked");
            }
            Console.Out.WriteLine("zone.SecurityZone = {0}", zone.SecurityZone);
        }

        public void DetectWithIoNtfs()
        {
            var fileInfo = new FileInfo(path);

            foreach (var alternateDataStream in fileInfo.ListAlternateDataStreams())
            {
                Console.WriteLine("{0} - {1}", alternateDataStream.Name, alternateDataStream.Size);
            }

            // Read the "Zone.Identifier" stream, if it exists:
            if (fileInfo.AlternateDataStreamExists("Zone.Identifier"))
            {
                Console.WriteLine("Found zone identifier stream:");

                var s = fileInfo.GetAlternateDataStream("Zone.Identifier",FileMode.Open);
                using (TextReader reader = s.OpenText())
                {
                    Console.WriteLine(reader.ReadToEnd());
                }
            }
            else
            {
                Console.WriteLine("No zone identifier stream found.");
            }
        }

        public void RemoveZoneIdentifier()
        {
            var fileInfo = new FileInfo(path);
            fileInfo.DeleteAlternateDataStream("Zone.Identifier");
        }

        public void SetZoneIdentifier()
        {
            var fileInfo = new FileInfo(path);

            var ads = new AlternateDataStreamInfo(path, "Zone.Identifier", null, false);
            using(var stream = ads.OpenWrite())
            using(var writer = new StreamWriter(stream))
            {
                writer.WriteLine("[ZoneTransfer]");
                writer.WriteLine("ZoneId=3");
            }
        }
    }
}