using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ScePSX.CdRom
{
    public class CdDisk
    {
        public List<CdTrack> Tracks;
        public bool HasCue;
        public bool HasDataTracks;
        public bool HasAudioTracks;
        public bool IsAudioDisk => HasAudioTracks && !HasDataTracks;
        public bool IsValid => Tracks != null;
        public string DiskID;

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

        private void ParseCue(string filepath, ref List<CdTrack> tracks)
        {
            Encoding encoding = DetectEncoding(filepath);
            string cueSheet = File.ReadAllText(filepath, encoding);
            string imageFolder = Path.GetDirectoryName(filepath);

            HasDataTracks = false;
            HasAudioTracks = false;

            int ParseMSF(string msf)
            {
                if (string.IsNullOrEmpty(msf))
                    return 0;
                var parts = msf.Split(':');
                int m = int.Parse(parts[0]);
                int s = int.Parse(parts[1]);
                int f = int.Parse(parts[2]);
                return m * 60 * 75 + s * 75 + f;
            }

            string FrameToMSF(int frame)
            {
                int m = frame / (60 * 75);
                int s = (frame % (60 * 75)) / 75;
                int f = frame % 75;
                return $"{m:D2}:{s:D2}:{f:D2}";
            }

            var fileMatches = Regex.Matches(cueSheet, @"FILE\s+""(.+?)""\s+(BINARY|MOTOROLA|AIFF|WAVE)");
            List<Tuple<int, int, string>> fileBlocks = new List<Tuple<int, int, string>>();
            for (int i = 0; i < fileMatches.Count; i++)
            {
                int start = fileMatches[i].Index;
                int end = (i < fileMatches.Count - 1) ? fileMatches[i + 1].Index : cueSheet.Length;
                string filename = fileMatches[i].Groups[1].Value;
                fileBlocks.Add(Tuple.Create(start, end, filename));
            }

            int globalFrameOffset = 0;
            Dictionary<string, long> fileSizes = new Dictionary<string, long>();

            foreach (var fileBlock in fileBlocks)
            {
                string filePath = Path.Combine(imageFolder, fileBlock.Item3);
                fileSizes[fileBlock.Item3] = new FileInfo(filePath).Length;
            }

            foreach (var fileBlock in fileBlocks)
            {
                string filename = fileBlock.Item3;
                string filePath = Path.Combine(imageFolder, filename);
                long fileSize = fileSizes[filename];

                string blockContent = cueSheet.Substring(fileBlock.Item1, fileBlock.Item2 - fileBlock.Item1);

                var trackMatches = Regex.Matches(blockContent, @"TRACK\s+(\d+)\s+(\w+(?:/\d+)?)([\s\S]*?)(?=(?:TRACK\s+\d+|FILE|$))");

                if (trackMatches.Count == 0)
                    continue;

                string firstTrackType = trackMatches[0].Groups[2].Value;
                bool firstTrackIsAudio = firstTrackType.Equals("AUDIO", StringComparison.OrdinalIgnoreCase);
                int fileSectorSize = 2352;

                int totalFramesInFile = (int)(fileSize / fileSectorSize);

                Match index01Match, index00Match;
                for (int i = 0; i < trackMatches.Count; i++)
                {
                    Match trackMatch = trackMatches[i];
                    int trackNumber = int.Parse(trackMatch.Groups[1].Value);
                    string trackType = trackMatch.Groups[2].Value;
                    string trackContent = trackMatch.Groups[3].Value;
                    bool isAudio = trackType.Equals("AUDIO", StringComparison.OrdinalIgnoreCase);

                    index01Match = Regex.Match(trackContent, @"INDEX\s+01\s+(\d{2}:\d{2}:\d{2})");
                    if (!index01Match.Success)
                    {
                        index00Match = Regex.Match(trackContent, @"INDEX\s+00\s+(\d{2}:\d{2}:\d{2})");
                        if (index00Match.Success)
                        {
                            index01Match = index00Match;
                        } else
                        {
                            index01Match = Regex.Match("INDEX 01 00:00:00", @"INDEX\s+01\s+(\d{2}:\d{2}:\d{2})");
                        }
                    }
                    int index01Frame = ParseMSF(index01Match.Groups[1].Value);

                    int index00Frame = 0;
                    index00Match = Regex.Match(trackContent, @"INDEX\s+00\s+(\d{2}:\d{2}:\d{2})");
                    if (index00Match.Success)
                    {
                        index00Frame = ParseMSF(index00Match.Groups[1].Value);
                    }

                    int pregapFrames = 0;
                    var pregapMatch = Regex.Match(trackContent, @"PREGAP\s+(\d{2}:\d{2}:\d{2})");
                    if (pregapMatch.Success)
                    {
                        pregapFrames = ParseMSF(pregapMatch.Groups[1].Value);
                    } else if (index00Match.Success && index00Frame < index01Frame)
                    {
                        pregapFrames = index01Frame - index00Frame;
                    }

                    int absoluteFrame = globalFrameOffset + index01Frame;

                    int lengthInFrames;
                    if (i < trackMatches.Count - 1)
                    {
                        var nextTrackMatch = trackMatches[i + 1];
                        var nextIndex01Match = Regex.Match(nextTrackMatch.Groups[3].Value, @"INDEX\s+01\s+(\d{2}:\d{2}:\d{2})");
                        if (!nextIndex01Match.Success)
                        {
                            var nextIndex00Match = Regex.Match(nextTrackMatch.Groups[3].Value, @"INDEX\s+00\s+(\d{2}:\d{2}:\d{2})");
                            if (nextIndex00Match.Success)
                            {
                                nextIndex01Match = nextIndex00Match;
                            } else
                            {
                                nextIndex01Match = Regex.Match("INDEX 01 00:00:00", @"INDEX\s+01\s+(\d{2}:\d{2}:\d{2})");
                            }
                        }
                        int nextIndex01Frame = ParseMSF(nextIndex01Match.Groups[1].Value);
                        lengthInFrames = nextIndex01Frame - index01Frame;
                    } else
                    {
                        lengthInFrames = totalFramesInFile - index01Frame;
                    }

                    CdTrack track = new CdTrack(filePath, isAudio, trackNumber, FrameToMSF(absoluteFrame))
                    {
                        M = absoluteFrame / (60 * 75),
                        S = (absoluteFrame % (60 * 75)) / 75,
                        F = absoluteFrame % 75,
                        PregapFrames = pregapFrames,
                        Start = (absoluteFrame - pregapFrames) * fileSectorSize,
                        RoundedStart = (absoluteFrame - pregapFrames) * fileSectorSize,
                        Length = lengthInFrames * fileSectorSize,
                        PLength = (lengthInFrames * fileSectorSize) + (pregapFrames * fileSectorSize),
                        PStart = absoluteFrame * fileSectorSize
                    }
            ;

                    if (isAudio)
                        HasAudioTracks = true;
                    else
                        HasDataTracks = true;

                    if (trackNumber == 1 && !isAudio && string.IsNullOrEmpty(DiskID))
                    {
                        DiskID = ReadDiscId(filePath);
                        if (DiskID == "")
                        {
                            DiskID = CalcCRC32(filePath).ToString("X8");
                        }
                        Console.WriteLine("[CDROM] ID: " + DiskID);
                    }

                    Console.WriteLine($"[CDROM] {(isAudio ? "Audio" : "Data ")} Track {trackNumber} " +
                        $"Start {FrameToMSF(absoluteFrame)} " +
                        $"Length {track.Length / 1024} Kb " +
                        $"{(pregapFrames != 0 ? "Pregap" : "")}"
                        );

                    tracks.Add(track);
                }

                globalFrameOffset += totalFramesInFile;
            }
        }

        private string GetFileName(string firstLine)
        {
            string[] splitted = firstLine.Split('"');
            return splitted[1];
        }

        public static (int, int, int) BytesToMSF(int totalSize)
        {
            int totalFrames = totalSize / 2352;
            int M = totalFrames / (60 * 75);
            int S = (totalFrames % (60 * 75)) / 75;
            int F = (totalFrames % (60 * 75)) % 75;
            return (M, S, F);
        }

        public Encoding DetectEncoding(string filePath)
        {
            byte[] buffer = File.ReadAllBytes(filePath);
            int length = buffer.Length;
            bool isAscii = true;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                return Encoding.UTF8;

            if (buffer.Length >= 2)
            {
                if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                    return Encoding.Unicode; // UTF-16 LE

                if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                    return Encoding.BigEndianUnicode; // UTF-16 BE
            }

            if (buffer.Length >= 4)
            {
                if (buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF)
                    return Encoding.GetEncoding(12000); // UTF-32 LE

                if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
                    return Encoding.GetEncoding(12001); // UTF-32 BE
            }


            foreach (byte b in buffer)
            {
                if (b > 0x7F)
                {
                    isAscii = false;
                    break;
                }
            }

            if (isAscii)
            {
                return Encoding.ASCII;
            }

            Encoding[] encodings = new[]
            {
                Encoding.GetEncoding(936), // GBK / GB2312
                Encoding.GetEncoding(950), // Big5
                Encoding.GetEncoding(932), // Shift-JIS 
                Encoding.GetEncoding(28591), // ISO-8859-1
                Encoding.GetEncoding(1251), // CP1251
            };

            foreach (Encoding encoding in encodings)
            {
                if (encoding == null)
                    continue;

                try
                {
                    Decoder decoder = encoding.GetDecoder();
                    char[] chars = new char[encoding.GetCharCount(buffer, 0, length)];
                    decoder.GetChars(buffer, 0, length, chars, 0);
                    return encoding;
                } catch
                {
                }
            }

            return new UTF8Encoding(false);
        }

        public static uint CalcCRC32(string filename)
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

        public string ReadDiscId(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                (uint lba, uint size) = FindSystemCnfMetadata(fs);
                if (lba == 0)
                    return "";

                int sectorSize = 0;
                long fileSize = fs.Length;
                if (fileSize % 2352 == 0)
                    sectorSize = 2352;
                if (fileSize % 2048 == 0)
                    sectorSize = 2048;
                if (sectorSize == 0)
                    return "";

                int dataOffset = sectorSize == 2352 ? 16 : 0;
                long filePosition = (lba * sectorSize) + dataOffset;

                fs.Seek(filePosition, SeekOrigin.Begin);
                byte[] fileData = new byte[size];
                int bytesRead = fs.Read(fileData, 0, (int)size);
                if (bytesRead != size)
                    return "";

                string text = Encoding.ASCII.GetString(fileData);
                foreach (string line in text.Split(new[] { '\0', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.Trim().StartsWith("BOOT", StringComparison.OrdinalIgnoreCase))
                    {
                        var match = Regex.Match(line, @"(?i)[\\:]([A-Z]{4}_\d{3}\.\d{2})");
                        if (match.Success)
                        {
                            string[] id = match.Groups[1].Value.Split(".");
                            return (id[0] + id[1]).Replace("_", "-");
                        }
                    }
                }
                return "";
            }
        }

        private static (uint Lba, uint Size) FindSystemCnfMetadata(FileStream fs)
        {
            byte[] buffer = new byte[1024 * 1024]; // 1MB 缓冲区
            long position = 0;

            while (position < fs.Length)
            {
                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < bytesRead - 64; i++) // 目录条目至少 33 字节
                {
                    int entryLength = buffer[i];
                    if (entryLength == 0 || i + entryLength > bytesRead)
                        continue;

                    int nameLength = buffer[i + 32];
                    if (nameLength < 11)
                        continue; // "SYSTEM.CNF" 至少 10 字符

                    string fileName;
                    try
                    {

                        fileName = Encoding.ASCII.GetString(
                            buffer,
                            i + 33,
                            nameLength
                        ).Split(';')[0].Trim().ToUpper();
                    } catch
                    {
                        return (0, 0);
                    }


                    if (fileName == "SYSTEM.CNF")
                    {
                        uint lba = BitConverter.ToUInt32(buffer, i + 2);
                        uint size = BitConverter.ToUInt32(buffer, i + 10);
                        return (lba, size);
                    }
                }
                position += bytesRead;
            }
            return (0, 0);
        }

    }
}
