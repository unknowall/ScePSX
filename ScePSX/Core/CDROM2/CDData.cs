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

        public string DiskID = "";

        public CDData(string diskFilename)
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
            } else
            {

                return;
            }

            streams = new FileStream[tracks.Count];

            for (var i = 0; i < tracks.Count; i++)
            {
                streams[i] = new FileStream(tracks[i].File, FileMode.Open, FileAccess.Read);
                if (DiskID == "")
                {
                    try
                    {
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
            // BUG we can still hear some garbage during audio track transitions, should the buffer be cleared when we're in a pre-gap?

            var track = getTrackFromLoc(loc);

            //Console.WriteLine("Loc: " + loc + " TrackLbaStart: " + currentTrack.lbaStart);
            //Console.WriteLine("readPos = " + (loc - currentTrack.lbaStart));

            var position = loc - track.LbaStart;
            if (position < 0)
                position = 0;

            var stream = streams[track.Index - 1];

            if (isCue)
            {
                position -= PRE_GAP;
                if (track.Indices.Count > 1)
                {
                    position += PRE_GAP; // assuming .CUE is compliant, i.e. two INDEX for an audio track
                }
            }

            position = (int)(position * BYTES_PER_SECTOR_RAW + track.FilePosition); // correct seek for any .BIN flavor

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
            var lba = 150; // BUG see if this is still needed because of the new way of .CUE is being parsed

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
                            return id[0] + id[1];
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

        public static List<CDTrack> FromBin(string file)
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

        public static List<CDTrack> FromCue(string path)
        // NOTE: this parsing outputs exactly like IsoBuster would
        {
            if (string.IsNullOrWhiteSpace(path))
                Console.WriteLine("[CDROM] Value cannot be null or whitespace.", nameof(path));

            Console.WriteLine($"[CDROM] Generating CD Tracks for: {Path.GetFileName(path)}");

            var directory = Path.GetDirectoryName(path);// ?? throw new NotImplementedException(); // TODO root case

            using var reader = new StreamReader(path);

            var tracks = new List<CDTrack>();

            const RegexOptions options = RegexOptions.Singleline | RegexOptions.Compiled;

            var rf = new Regex(@"^\s*FILE\s+("".*"")\s+BINARY\s*$", options);
            var rt = new Regex(@"^\s*TRACK\s+(\d{2})\s+(MODE2/2352|AUDIO)\s*$", options);
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
                        Console.WriteLine($"[CDROM] TRACK at line {lineNumber} does not have a parent FILE.");

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
                        Console.WriteLine($"[CDROM] INDEX at line {lineNumber} does not have a parent TRACK.");

                    var n = Convert.ToInt32(indexMatch.Groups[1].Value);
                    var m = Convert.ToInt32(indexMatch.Groups[2].Value);
                    var s = Convert.ToInt32(indexMatch.Groups[3].Value);
                    var f = Convert.ToInt32(indexMatch.Groups[4].Value);

                    currentTrack.Indices.Add(new TrackIndex(n, new TrackPosition(m, s, f)));

                    continue;
                }
            }

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
