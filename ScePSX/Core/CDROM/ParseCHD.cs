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
        private void ParseCHD(ChdReader chdReader, ref List<CdTrack> tracks)
        {
            HasAudioTracks = false;
            HasDataTracks = false;
            int TotalFrams = 0;

            foreach (ChdTrack track in chdReader.Tracks)
            {
                var startpos = (int)(chdReader.GetTrackDataStartSector(track.TrackNumber - 1) * track.SectorDataSize);
                var pregappos = startpos - (track.Pregap * track.SectorDataSize);

                if (track.IsAudio)
                    HasAudioTracks = true;
                else
                    HasDataTracks = true;

                var CurrTotalFrams = TotalFrams + track.Pregap;

                CdTrack dataTrack = new CdTrack(chdReader.DiscPath, track.IsAudio, track.TrackNumber, FrameToMSF(CurrTotalFrams), chdReader)
                {
                    M = CurrTotalFrams / (60 * 75),
                    S = (CurrTotalFrams % (60 * 75)) / 75,
                    F = CurrTotalFrams % 75,
                    PregapFrames = track.Pregap,
                    Start = pregappos,
                    RoundedStart = pregappos,
                    Length = (track.Frames - track.Pregap) * track.SectorDataSize,
                    IncludePregapLength = track.Frames * track.SectorDataSize,
                    IncludePregapStart = startpos
                };

                Console.WriteLine($"[CHD] {(track.IsAudio ? "Audio" : "Data ")} Track {track.TrackNumber} " +
                    $"Start {FrameToMSF(CurrTotalFrams)} " +
                    //$"Pos 0x{dataTrack.RoundedStart:X} " +
                    $"Length {dataTrack.Length / 1024} Kb " +
                    $"{(track.Pregap != 0 ? "Pregap" : "")}"
                    );

                TotalFrams += track.Frames;

                tracks.Add(dataTrack);
            }

            DiskID = ReadDiscId(chdReader);
        }

        public string ReadDiscId(ChdReader chdReader)
        {
            var tarck = chdReader.Tracks[0];
            if (!tarck.IsData)
                return "";

            (uint lba, uint size) = FindSystemCnfMetadata(chdReader);
            if (lba == 0)
                return "";

            int dataOffset = tarck.SectorDataSize == 2352 ? 16 : 0;
            byte[] fileData = new byte[tarck.SectorDataSize];
            int bytesRead = chdReader.ReadSector(lba, fileData);
            if (bytesRead == 0)
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

        private (uint Lba, uint Size) FindSystemCnfMetadata(ChdReader chdReader)
        {
            byte[] buffer = new byte[1024 * 1024];
            long position = 0;
            var tarck = chdReader.Tracks[0];

            while (position < tarck.Frames)
            {
                int bytesRead = chdReader.ReadSector(position, buffer);
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
                position++;
            }
            return (0, 0);
        }
    }
}
