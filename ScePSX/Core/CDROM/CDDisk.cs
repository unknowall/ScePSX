using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Libchdr;
using MessagePack;

namespace ScePSX.CdRom
{
    public partial class CdDisk
    {
        public List<CdTrack> Tracks;
        public bool HasCue;
        public bool HasDataTracks;
        public bool HasAudioTracks;
        public bool IsAudioDisk => HasAudioTracks && !HasDataTracks;
        public bool IsValid => Tracks != null;
        public string DiskID;
        public bool HasCHD;

        public CdDisk()
        {
        }

        public CdDisk(string filepath, string diskid)
        {
            this.DiskID = diskid;
            Tracks = BuildTracks(filepath);
        }

        private List<CdTrack> BuildTracks(string filepath)
        {
            List<CdTrack> tracks = new List<CdTrack>();
            if (Path.GetExtension(filepath).ToLower().Equals(".chd"))
            {
                HasCue = false;
                ChdReader chdReader = new ChdReader();
                HasCHD = chdReader.Open(filepath);
                if (!HasCHD)
                {
                    DiskID = "";
                    return tracks;
                }
                ParseCHD(chdReader, ref tracks);
                //chdReader.Dispose();
                return tracks;
            } else
            if (Path.GetExtension(filepath).ToLower().Equals(".cue"))
            {
                HasCue = true;
                ParseCue(filepath, ref tracks);
                return tracks;
            } else
            {
                if (DiskID == "")
                    DiskID = ReadDiscId(filepath);
                if (DiskID == "")
                {
                    DiskID = CalcCRC32(filepath).ToString("X8");
                }
                HasDataTracks = true;
                CdTrack dataTrack = new CdTrack(filepath, false, 01, "00:00:00");
                tracks.Add(dataTrack);
                dataTrack.Length = (int)new FileInfo(dataTrack.FilePath).Length;
                return tracks;
            }
        }

        public int ParseMSF(string msf)
        {
            if (string.IsNullOrEmpty(msf))
                return 0;
            var parts = msf.Split(':');
            int m = int.Parse(parts[0]);
            int s = int.Parse(parts[1]);
            int f = int.Parse(parts[2]);
            return m * 60 * 75 + s * 75 + f;
        }

        public string FrameToMSF(int frame)
        {
            int m = frame / (60 * 75);
            int s = (frame % (60 * 75)) / 75;
            int f = frame % 75;
            return $"{m:D2}:{s:D2}:{f:D2}";
        }

        public (int, int, int) BytesToMSF(int totalSize)
        {
            int totalFrames = totalSize / 2352;
            int M = totalFrames / (60 * 75);
            int S = (totalFrames % (60 * 75)) / 75;
            int F = (totalFrames % (60 * 75)) % 75;
            return (M, S, F);
        }

        public uint CalcCRC32(string filename)
        {
            uint[] crc32Table = new uint[256];
            const uint polynomial = 0xEDB88320;

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ polynomial;
                    else
                        crc >>= 1;
                }
                crc32Table[i] = crc;
            }

            uint crcValue = 0xFFFFFFFF;
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        byte index = (byte)((crcValue ^ buffer[i]) & 0xFF);
                        crcValue = (crcValue >> 8) ^ crc32Table[index];
                    }
                }
            }

            return crcValue ^ 0xFFFFFFFF;
        }
    }
}
