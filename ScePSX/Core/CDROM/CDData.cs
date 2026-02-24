using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Libchdr;
using MessagePack;

namespace ScePSX.CdRom
{
    public class CdTrack
    {
        public int TrackNumber;
        public bool IsAudioTrack;
        public string FilePath;
        public string Index01MSF;
        public int M;
        public int S;
        public int F;
        public int PregapFrames;
        public int Start;
        public int RoundedStart;

        public int Length;
        public int IncludePregapStart;
        public int IncludePregapLength;

        public const int BYTES_PER_SECTOR_RAW = 2352;
        public byte[] SectorBuffer = new byte[BYTES_PER_SECTOR_RAW];

        [IgnoreMember]
        private FileStream fs;

        [IgnoreMember]
        public ChdReader? chdReader = null;

        public CdTrack()
        {
        }

        public CdTrack(string path, bool isAudio, int trackNumber, string index1, ChdReader? chdReader = null)
        {
            FilePath = path;
            IsAudioTrack = isAudio;
            TrackNumber = trackNumber;
            Index01MSF = index1;
            this.chdReader = chdReader;

            if (chdReader == null)
                fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
        }

        public void LoadFs()
        {
            if (chdReader == null)
                fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
        }

        public void ReadSector(int position)
        {
            if (chdReader == null)
            {
                if (position < 0)
                    position = 0;
                if (fs.Seek(position, SeekOrigin.Begin) == -1)
                    Console.WriteLine("[CDROM] ReadSector: Seek failed at position " + position);
                fs.Read(SectorBuffer, 0, SectorBuffer.Length);
            } else
            {
                var lba = position / BYTES_PER_SECTOR_RAW;
                chdReader.ReadSector(lba, SectorBuffer);
            }
        }
    }

    public class CDData
    {
        public CdDisk Disk;
        public Sector LastReadSector;
        public Queue<byte> DataFifo = new Queue<byte>();
        public uint BytesToSkip = 12;
        public uint SizeOfDataSegment = 0x800;
        public bool XA_ADPCM_En = false;
        public XAFilter Filter;
        public XaVolume CurrentVolume;
        public XaAdpcm ADPCMDecoder = new XaAdpcm();
        public Queue<short> CDAudioSamples = new Queue<short>();

        public CdTrack SelectedTrack;

        public string DiskID;

        public int SelectedTrackNumber = 1;
        public byte[] LastSectorHeader = new byte[0x4];
        public byte[] LastSectorSubHeader = new byte[0x4];
        public byte Padding;
        public int BFRD = 0;
        public int EndOfTrack => Disk.Tracks[SelectedTrackNumber - 1].RoundedStart + Disk.Tracks[SelectedTrackNumber - 1].Length;
        public int EndOfDisk;

        public struct Sector
        {
            public int TrackNumber;
            public int Start;
            public int Length;
        }

        public struct XAFilter
        {
            public bool IsEnabled;
            public byte fileNumber;
            public byte channelNumber;
        }

        enum SectorType
        {
            Video = 1, Audio = 2, Data = 4
        }

        [IgnoreMember]
        public ChdReader? chdReader = null;

        public string DiskPath = "";

        public CDData()
        {
        }

        public CDData(string diskPath = "", string diskid = "")
        {
            DiskPath = diskPath;
            this.DiskID = diskid;
            LoadDisk(diskPath);
            CurrentVolume.RtoL = 0x40;
            CurrentVolume.RtoR = 0x40;
            CurrentVolume.LtoL = 0x40;
            CurrentVolume.RtoR = 0x40;
        }

        public int CalcEndOfDisk()
        {
            if (Disk.Tracks.Count == 0)
                return 0;

            CdTrack lastTrack = Disk.Tracks.Last();
            int endPosition = lastTrack.RoundedStart + lastTrack.Length + (lastTrack.PregapFrames * 2352);
            return endPosition;
        }

        public void LoadDisk(string diskPath = "")
        {
            if (diskPath != "")
            {
                Disk = new CdDisk(diskPath, DiskID);
                if (Disk.HasCHD)
                {
                    chdReader = Disk.Tracks[0].chdReader;
                }
                if (Disk.IsValid)
                {
                    this.DiskID = Disk.DiskID;
                    SelectTrackAndRead(1, 0);
                    EndOfDisk = CalcEndOfDisk();
                    //Console.WriteLine($"[CDROM] Totla Length {EndOfDisk / 1024} Kb, Total MSF {CdDisk.BytesToMSF(EndOfDisk)}");
                } else
                {
                    Console.WriteLine("[CDROM] Invalid Disk!");
                }
                if (Disk.IsAudioDisk)
                {
                    Console.WriteLine("[CDROM] Audio Disk detected");
                }
            }
        }

        public void ReLoadFS(string Rom)
        {
            if (Disk.Tracks == null)
                return;
            DiskPath = Rom;
            if (Disk.HasCHD)
            {
                chdReader = new ChdReader();
                chdReader.Open(DiskPath);
                foreach (var track in Disk.Tracks)
                {
                    track.FilePath = DiskPath;
                    track.chdReader = chdReader;
                }
                SelectedTrack.chdReader = chdReader;
            } else
            {
                foreach (CdTrack track in Disk.Tracks)
                {
                    track.FilePath = DiskPath;
                    track.LoadFs();
                }
                SelectedTrack.FilePath = DiskPath;
                SelectedTrack.LoadFs();
            }
        }

        public uint ReadWord()
        {
            if (BFRD != 1 || DataFifo.Count == 0)
            {
                return 0;
            }
            uint b0 = DataFifo.Dequeue();
            uint b1 = DataFifo.Dequeue();
            uint b2 = DataFifo.Dequeue();
            uint b3 = DataFifo.Dequeue();
            uint word = b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
            return word;
        }

        public byte ReadByte()
        {
            if (BFRD != 1 || DataFifo.Count == 0)
            {
                return 0;
            }
            return DataFifo.Dequeue();
        }

        public void MoveSectorToDataFifo()
        {
            if (LastReadSector.TrackNumber == SelectedTrackNumber)
            {
                for (int i = 0; i < LastReadSector.Length; i++)
                {
                    DataFifo.Enqueue(SelectedTrack.SectorBuffer[LastReadSector.Start + i]);
                }
            } else
            {
                CdTrack tempTrack = Disk.Tracks[LastReadSector.TrackNumber - 1];
                tempTrack.ReadSector(LastReadSector.Start);
                for (int i = 0; i < LastReadSector.Length; i++)
                {
                    DataFifo.Enqueue(tempTrack.SectorBuffer[LastReadSector.Start + i]);
                }
            }
        }

        public bool LoadNewSector(int currentPos)
        {
            SelectedTrack.ReadSector(currentPos);

            LastSectorHeader[0] = SelectedTrack.SectorBuffer[0x0C];
            LastSectorHeader[1] = SelectedTrack.SectorBuffer[0x0D];
            LastSectorHeader[2] = SelectedTrack.SectorBuffer[0x0E];
            LastSectorHeader[3] = SelectedTrack.SectorBuffer[0x0F];
            byte fileNumber = LastSectorSubHeader[0] = SelectedTrack.SectorBuffer[0x10];
            byte channelNumber = LastSectorSubHeader[1] = SelectedTrack.SectorBuffer[0x11];
            byte subMode = LastSectorSubHeader[2] = SelectedTrack.SectorBuffer[0x12];
            byte codingInfo = LastSectorSubHeader[3] = SelectedTrack.SectorBuffer[0x13];

            ReadOnlySpan<byte> fullSector = new ReadOnlySpan<byte>(SelectedTrack.SectorBuffer);
            SectorType sectorType = (SectorType)((subMode >> 1) & 0x7);

            if (XA_ADPCM_En && sectorType == SectorType.Audio)
            {

                if (Filter.IsEnabled)
                {
                    if (Filter.fileNumber == fileNumber && Filter.channelNumber == channelNumber)
                    {
                        ADPCMDecoder.handle_XA_ADPCM(fullSector, codingInfo, CurrentVolume, ref CDAudioSamples);
                    }
                } else
                {
                    ADPCMDecoder.handle_XA_ADPCM(fullSector, codingInfo, CurrentVolume, ref CDAudioSamples);
                }
                return false;
            } else
            {
                //Data sector (or audio but disabled)
                //Should continue to data path
                uint size = SizeOfDataSegment;
                LastReadSector.TrackNumber = SelectedTrackNumber;
                LastReadSector.Start = (int)(BytesToSkip);// + currentPos);
                LastReadSector.Length = (int)size;
                return true;
            }
        }

        private void ByteswapSectorBuffer(Span<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i += 2)
            {
                byte temp = buffer[i];
                buffer[i] = buffer[i + 1];
                buffer[i + 1] = temp;
            }
        }

        public void PlayCDDA(int currentPos)
        {
            if (!Disk.HasAudioTracks)
            {
                Console.WriteLine("[CDROM] CD-DA with Not AudioTracks!");
                return;
            }

            if (chdReader != null)
            {
                SelectedTrack.ReadSector(currentPos);
                //CHD flac decode is little-endian
                ByteswapSectorBuffer(SelectedTrack.SectorBuffer);
            } else
            {
                int offset = currentPos - Disk.Tracks[SelectedTrackNumber - 1].RoundedStart;
                if (offset >= SelectedTrack.IncludePregapLength && Disk.Tracks.Count > 1)
                {
                    int newTrack = FindTrack(currentPos, 1);
                    offset = (currentPos - Disk.Tracks[SelectedTrackNumber - 1].RoundedStart);
                    SelectTrackAndRead(newTrack, offset);
                    Console.WriteLine("[CDROM] CD-DA Moving to track: " + SelectedTrackNumber);
                } else if (offset < 0 && Disk.Tracks.Count > 1)
                {
                    int newTrack = FindTrack(currentPos, 1);
                    if (newTrack == SelectedTrackNumber)
                    {
                        Console.WriteLine("[CDROM] CD-DA Pregap!");
                        return;
                    } else
                    {
                        offset = currentPos - Disk.Tracks[SelectedTrackNumber - 1].RoundedStart;
                        SelectTrackAndRead(newTrack, offset);
                    }
                } else
                {
                    SelectedTrack.ReadSector(offset);
                }
            }

            ReadOnlySpan<byte> fullSector = new ReadOnlySpan<byte>(SelectedTrack.SectorBuffer);
            for (int i = 0; i < fullSector.Length; i += 4)
            {
                short L = MemoryMarshal.Read<short>(fullSector.Slice(i, 2));
                short R = MemoryMarshal.Read<short>(fullSector.Slice(i + 2, 2));
                CDAudioSamples.Enqueue(L);
                CDAudioSamples.Enqueue(R);
            }
        }

        public void SelectTrackAndRead(int trackNumber, int position)
        {
            SelectedTrack = Disk.Tracks[trackNumber - 1];
            SelectedTrack.ReadSector(position);
            SelectedTrackNumber = Disk.Tracks[trackNumber - 1].TrackNumber;
        }

        public int FindTrack(int Pos, int StartTrack = 0)
        {
            if (Disk.Tracks.Count == 1)
                return 1;

            for (int i = StartTrack; i < Disk.Tracks.Count; i++)
            {
                if (Pos < (Disk.Tracks[i].RoundedStart + Disk.Tracks[i].IncludePregapLength))
                {
                    return Disk.Tracks[i].TrackNumber;
                }
            }

            Console.WriteLine($"[CDROM] FindTrack FAIL On {Pos}");
            return 1;
        }
    }
}
