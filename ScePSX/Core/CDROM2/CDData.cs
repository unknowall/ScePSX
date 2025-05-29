using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScePSX.CdRom2
{
    [Serializable]
    public class CDData
    {
        private const int BYTES_PER_SECTOR_RAW = 2352;
        private const int PRE_GAP = 150;

        private byte[] rawSectorBuffer = new byte[BYTES_PER_SECTOR_RAW];

        [NonSerialized]
        private FileStream[] streams;

        public bool isTrackChange;

        public List<CDTrack> tracks;

        public bool isCue;

        public bool isMultipleFile;

        public string DiskID = "";

        public CDData(string diskFilename, string diskid = "")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            var ext = Path.GetExtension(diskFilename);

            if (ext == ".bin" || ext == ".iso")
            {
                isCue = false;
                tracks = FromBin(diskFilename);
            } else if (ext == ".cue")
            {
                isCue = true;
                tracks = FromCue(diskFilename);
            } else if (ext == ".exe")
            {
                DiskID = CalcCRC32(diskFilename).ToString();
                return;
            }

            if (diskid != "")
                DiskID = diskid;

            streams = new FileStream[tracks.Count];

            for (var i = 0; i < tracks.Count; i++)
            {
                streams[i] = new FileStream(tracks[i].File, FileMode.Open, FileAccess.Read);
                if (DiskID == "")
                {
                    try
                    {
                        if (Path.GetExtension(tracks[i].File) == ".img")
                        {
                            DiskID = $"{CalcCRC32(tracks[i].File):X8}";
                        } else
                            DiskID = ReadDiscId(tracks[i].File);
                    } catch
                    {
                        return;
                    }
                }
                Console.WriteLine($"[CDROM] Track [{i}] {Path.GetFileName(tracks[i].File)} {tracks[i].FileLength / 1024} KB, LBA {tracks[i].LbaStart} - {tracks[i].LbaEnd}");
            }

            Console.ResetColor();
        }

        public void LoadFileStream()
        {
            streams = new FileStream[tracks.Count];

            for (int i = 0; i < tracks.Count; i++)
            {
                streams[i] = new FileStream(tracks[i].File, FileMode.Open, FileAccess.Read);
            }
        }

        public byte[] Read(int loc)
        {
            var track = getTrackFromLoc(loc);

            //Console.WriteLine("Loc: " + loc + " TrackLbaStart: " + currentTrack.lbaStart);
            //Console.WriteLine("readPos = " + (loc - currentTrack.lbaStart));

            var position = loc - track.LbaStart;
            if (position < 0)
                position = 0;

            var stream = streams[track.Index - 1];

            if (isCue /*&& track.Index == 1*/ && position >= PRE_GAP && track.Indices.Count <= 1)
            {
                position -= PRE_GAP;
            }

            position = (int)(position * BYTES_PER_SECTOR_RAW);

            stream.Seek(position, SeekOrigin.Begin);

            var size = rawSectorBuffer.Length;
            var read = stream.Read(rawSectorBuffer, 0, size);

            if (read != size)
            {
                Console.WriteLine($"[CDROM] ERROR: Could only read {read} of {size} bytes from {stream.Name}.");
            }

            return rawSectorBuffer;
        }

        public CDTrack getTrackFromLoc(int loc)
        {
            foreach (var track in tracks)
            {
                isTrackChange = loc == track.LbaEnd;
                //Console.WriteLine(loc + " " + track.number + " " + track.lbaEnd + " " + isTrackChange);
                if (track.LbaEnd >= loc)
                    return track;
            }

            Console.WriteLine("[CDROM] WARNING: LBA beyond tracks!");
            return tracks[0];
        }

        public int getLBA()
        {
            var lba = 150;

            foreach (var track in tracks)
            {
                lba += track.LbaLength;
            }

            Console.WriteLine($"[CDROM] LBA: {lba:x8}");
            return lba;
        }

        public static string ReadDiscId(string filePath)
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

                    string fileName = Encoding.ASCII.GetString(
                        buffer,
                        i + 33,
                        nameLength
                    ).Split(';')[0].Trim().ToUpper();

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
                Encoding.GetEncoding(1251), // RU
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

        public List<CDTrack> FromBin(string file)
        {
            var tracks = new List<CDTrack>();

            var size = new FileInfo(file).Length;
            var lba = (int)(size / BYTES_PER_SECTOR_RAW);
            var lbaStart = 150; // 150 frames (2 seconds) offset from track 1
            var lbaEnd = lba;
            byte number = 1;

            tracks.Add(new CDTrack(file, size, number, lbaStart, lbaEnd));

            return tracks;
        }

        public List<CDTrack> FromCue(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                Console.WriteLine("[CDROM] Value cannot be null or whitespace.", nameof(path));

            Console.WriteLine($"[CDROM] Generating CD Tracks for: {Path.GetFileName(path)}");

            var directory = Path.GetDirectoryName(path);

            Encoding fileEncoding = DetectEncoding(path);

            Console.WriteLine($"[CDROM] Cue Encoding: {fileEncoding.CodePage.ToString()}");

            using var reader = new StreamReader(path, fileEncoding);

            var tracks = new List<CDTrack>();

            const RegexOptions options = RegexOptions.Singleline | RegexOptions.Compiled;

            var rf = new Regex(@"^\s*FILE\s+("".*"")\s+BINARY\s*$", options);
            var rt = new Regex(@"^\s*TRACK\s+(\d{2})\s+(MODE1/2352|MODE2/2352|AUDIO)\s*$", options);
            var ri = new Regex(@"^\s*INDEX\s+(\d{2})\s+(\d{2}):(\d{2}):(\d{2})\s*$", options);

            var files = new HashSet<string>();

            var currentFile = default(string);
            var currentTrack = default(CDTrack);

            string line;

            var lineNumber = 0;

            while ((line = reader.ReadLine()) is not null)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var fileMatch = rf.Match(line);
                if (fileMatch.Success)
                {
                    files.Add(currentFile = Path.Combine(directory, fileMatch.Groups[1].Value.Trim('"')));
                    continue;
                }

                var trackMatch = rt.Match(line);
                if (trackMatch.Success)
                {
                    if (currentFile is null)
                    {
                        Console.WriteLine($"[CDROM] TRACK at line {lineNumber} does not have a parent FILE.");
                        return null;
                    }

                    currentTrack = new CDTrack
                    {
                        File = currentFile,
                        Index = Convert.ToByte(trackMatch.Groups[1].Value)
                    };

                    tracks.Add(currentTrack);

                    continue;
                }

                var indexMatch = ri.Match(line);
                if (indexMatch.Success)
                {
                    if (currentTrack is null)
                    {
                        Console.WriteLine($"[CDROM] INDEX at line {lineNumber} does not have a parent TRACK.");
                        return null;
                    }

                    var n = Convert.ToInt32(indexMatch.Groups[1].Value);
                    var m = Convert.ToInt32(indexMatch.Groups[2].Value);
                    var s = Convert.ToInt32(indexMatch.Groups[3].Value);
                    var f = Convert.ToInt32(indexMatch.Groups[4].Value);

                    currentTrack.Indices.Add(new TrackIndex(n, new TrackPosition(m, s, f)));

                    continue;
                }
            }

            isMultipleFile = files.Count > 1;

            if (files.Count is 1)
            {
                var length = new FileInfo(files.Single()).Length;

                for (var i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];

                    track.FileLength = length;

                    track.LbaStart = track.Indices.Last().Position.ToInt32();

                    if (i == tracks.Count - 1)
                    {
                        track.LbaEnd = (int)(length / BYTES_PER_SECTOR_RAW - 1);
                    } else
                    {
                        track.LbaEnd = tracks[i + 1].Indices.First().Position.ToInt32() - 1;
                    }

                    track.LbaLength = track.LbaEnd - track.LbaStart + 1;

                    track.FilePosition = track.LbaStart * BYTES_PER_SECTOR_RAW;
                }
            } else
            {
                var lba = 0;

                foreach (var track in tracks)
                {
                    var length = new FileInfo(track.File).Length;
                    var blocks = length / BYTES_PER_SECTOR_RAW;

                    track.LbaStart = lba;

                    track.FileLength = length;

                    foreach (var index in track.Indices)
                    {
                        track.LbaStart += index.Position.ToInt32(); // pre-gap
                    }

                    track.LbaEnd = (int)(track.LbaStart + blocks - 1);

                    foreach (var index in track.Indices)
                    {
                        track.LbaEnd -= index.Position.ToInt32(); // pre-gap
                    }

                    track.LbaLength = track.LbaEnd - track.LbaStart + 1;

                    track.FilePosition = track.LbaStart * BYTES_PER_SECTOR_RAW;

                    lba += (int)blocks;
                }
            }

            return tracks;
        }

    }

}
