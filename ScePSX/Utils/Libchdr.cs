using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Libchdr
{
    public enum ChdError : int
    {
        None = 0,
        NoInterface = 1,
        OutOfMemory = 2,
        InvalidFile = 3,
        InvalidParameter = 4,
        InvalidData = 5,
        FileNotFound = 6,
        RequiresParent = 7,
        FileNotWriteable = 8,
        ReadError = 9,
        WriteError = 10,
        CodecError = 11,
        InvalidParent = 12,
        HunkOutOfRange = 13,
        DecompressionError = 14,
        CompressionError = 15,
        CantCreateFile = 16,
        CantVerify = 17,
        NotSupported = 18,
        MetadataNotFound = 19,
        InvalidMetadataSize = 20,
        UnsupportedVersion = 21,
        VerifyIncomplete = 22,
        InvalidMetadata = 23,
        InvalidState = 24,
        OperationPending = 25,
        NoAsyncOperation = 26,
        UnsupportedFormat = 27
    }

    public class ChdHeader
    {
        public uint Version;
        public uint HunkBytes;
        public uint TotalHunks;
        public ulong LogicalBytes;
        public uint UnitBytes;
    }

    public class ChdTrack
    {
        public int TrackNumber;
        public string Type;
        public string SubType;
        public int Frames;
        public int Pad;
        public int Pregap;
        public string PregapType;
        public string PregapSubType;
        public int Postgap;

        public bool IsAudio => string.Equals(Type, "AUDIO", StringComparison.OrdinalIgnoreCase);
        public bool IsData => !IsAudio;

        public int SectorDataSize => Type?.ToUpperInvariant() switch
        {
            "AUDIO" => 2352,
            "MODE1" => 2048,
            "MODE1_RAW" => 2352,
            "MODE1/2048" => 2048,
            "MODE1/2352" => 2352,
            "MODE2" => 2336,
            "MODE2_RAW" => 2352,
            "MODE2/2336" => 2336,
            "MODE2/2352" => 2352,
            "MODE2_FORM1" => 2048,
            "MODE2/2048" => 2048,
            "MODE2_FORM2" => 2328,
            "MODE2/2324" => 2324,
            _ => 2352
        };
    }

    public static class NativeMethods
    {
        private const string LibraryName = "libchdr";

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ChdError chd_open(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
            int mode,
            IntPtr parent,
            out IntPtr chd);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr chd_get_header(IntPtr chd);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ChdError chd_read(IntPtr chd, uint hunknum, IntPtr buffer);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ChdError chd_get_metadata(
            IntPtr chd,
            uint searchtag,
            uint searchindex,
            IntPtr output,
            uint outputlen,
            out uint resultlen,
            out uint resulttag,
            out byte resultflags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void chd_close(IntPtr chd);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr chd_error_string(ChdError err);
    }

    public sealed class ChdReader : IDisposable
    {
        private const int CHD_OPEN_READ = 1;
        private const int ChdFrameSize = 2448; // 2352 data + 96 subcode
        private const int ChdSectorDataSize = 2352;
        private const int TrackPadding = 4; // chdman aligns each track to 4-frame boundaries

        private const uint CDROM_T2_METADATA_TAG = 0x43485432; // 'CHT2'
        private const uint CDROM_TR_METADATA_TAG = 0x43485452; // 'CHTR'
        private const uint CDROM_CD_METADATA_TAG = 0x43484344; // 'CHCD'

        private const int HeaderOffset_Version = 4;
        private const int HeaderOffset_HunkBytes = 28;
        private const int HeaderOffset_TotalHunks = 32;
        private const int HeaderOffset_LogicalBytes = 40;
        private const int HeaderOffset_UnitBytes = 156;

        private IntPtr _chdHandle;
        private bool _disposed;
        private IntPtr HunkBuffer = IntPtr.Zero;
        private int framesPerHunk;

        public string DiscPath;
        public ChdHeader Header;
        public IReadOnlyList<ChdTrack> Tracks;

        public ChdReader()
        {
        }

        public bool Open(string filePath)
        {
            try
            {
                DiscPath = filePath;
                var error = NativeMethods.chd_open(filePath, CHD_OPEN_READ, IntPtr.Zero, out _chdHandle);
                if (error == ChdError.RequiresParent)
                {
                    Console.WriteLine("CHDs are not supported");
                    return false;
                }
                if (error != ChdError.None)
                {
                    Console.WriteLine($"Failed to open file: {chd_error_string_safe(error)} ({error})");
                    return false;
                }
                ReadHeader();
                ReadTracks();

                HunkBuffer = Marshal.AllocHGlobal((int)Header.HunkBytes);
                framesPerHunk = (int)(Header.HunkBytes / ChdFrameSize);

            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }

        private void ReadHeader()
        {
            IntPtr headerPtr = NativeMethods.chd_get_header(_chdHandle);
            if (headerPtr == IntPtr.Zero)
                throw new Exception("Failed to read CHD header");

            Header = new ChdHeader
            {
                Version = (uint)Marshal.ReadInt32(headerPtr, HeaderOffset_Version),
                HunkBytes = (uint)Marshal.ReadInt32(headerPtr, HeaderOffset_HunkBytes),
                TotalHunks = (uint)Marshal.ReadInt32(headerPtr, HeaderOffset_TotalHunks),
                LogicalBytes = (ulong)Marshal.ReadInt64(headerPtr, HeaderOffset_LogicalBytes),
                UnitBytes = (uint)Marshal.ReadInt32(headerPtr, HeaderOffset_UnitBytes),
            };

            if (Header.Version < 1 || Header.Version > 5)
                throw new Exception($"Unsupported CHD version: {Header.Version}");
            if (Header.HunkBytes == 0)
                throw new Exception("Invalid CHD: hunkbytes is 0");
        }

        private void ReadTracks()
        {
            var tracks = new List<ChdTrack>();

            if (!TryReadTracksWithTag(CDROM_T2_METADATA_TAG, tracks))
            {
                if (!TryReadTracksWithTag(CDROM_TR_METADATA_TAG, tracks))
                {
                    TryReadTracksWithTag(CDROM_CD_METADATA_TAG, tracks);
                }
            }

            if (tracks.Count == 0)
                throw new Exception("No track metadata found.");

            tracks.Sort((a, b) => a.TrackNumber.CompareTo(b.TrackNumber));
            Tracks = tracks.AsReadOnly();
        }

        private bool TryReadTracksWithTag(uint tag, List<ChdTrack> tracks)
        {
            tracks.Clear();
            IntPtr buffer = Marshal.AllocHGlobal(512);
            try
            {
                for (uint index = 0; ; index++)
                {
                    var error = NativeMethods.chd_get_metadata(_chdHandle, tag, index, buffer, 512,
                        out uint resultLen, out _, out _);

                    if (error == ChdError.MetadataNotFound)
                        break;
                    if (error != ChdError.None)
                        break;

                    string metadata = Marshal.PtrToStringAnsi(buffer, (int)resultLen);
                    if (string.IsNullOrEmpty(metadata))
                        continue;

                    var track = ParseTrackMetadata(metadata);
                    if (track != null)
                        tracks.Add(track);
                }
            } finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return tracks.Count > 0;
        }

        private static ChdTrack ParseTrackMetadata(string metadata)
        {
            var track = new ChdTrack();
            var parts = metadata.TrimEnd('\0').Split(' ');
            foreach (var part in parts)
            {
                int colonIdx = part.IndexOf(':');
                if (colonIdx < 0)
                    continue;
                string key = part.Substring(0, colonIdx);
                string value = part.Substring(colonIdx + 1);

                switch (key)
                {
                    case "TRACK":
                        int.TryParse(value, out int tn);
                        track.TrackNumber = tn;
                        break;
                    case "TYPE":
                        track.Type = value;
                        break;
                    case "SUBTYPE":
                        track.SubType = value;
                        break;
                    case "FRAMES":
                        int.TryParse(value, out int fr);
                        track.Frames = fr;
                        break;
                    case "PAD":
                        int.TryParse(value, out int pd);
                        track.Pad = pd;
                        break;
                    case "PREGAP":
                        int.TryParse(value, out int pg);
                        track.Pregap = pg;
                        break;
                    case "PGTYPE":
                        track.PregapType = value;
                        break;
                    case "PGSUB":
                        track.PregapSubType = value;
                        break;
                    case "POSTGAP":
                        int.TryParse(value, out int psg);
                        track.Postgap = psg;
                        break;
                }
            }

            if (track.TrackNumber <= 0 || string.IsNullOrEmpty(track.Type))
                return null;

            return track;
        }

        public int ReadSector(long sectorIndex, byte[] sectorData)
        {
            uint hunkNum = (uint)(sectorIndex / framesPerHunk);
            int frameInHunk = (int)(sectorIndex % framesPerHunk);
            try
            {
                var error = NativeMethods.chd_read(_chdHandle, hunkNum, HunkBuffer);
                if (error != ChdError.None)
                    throw new Exception($"Failed to read CHD hunk {hunkNum}: {chd_error_string_safe(error)}");

                Marshal.Copy(HunkBuffer + (frameInHunk * ChdFrameSize), sectorData, 0, ChdSectorDataSize);
                return ChdSectorDataSize;
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return 0;
        }

        public byte[] ReadSectors(long startSector, int count)
        {
            byte[] result = new byte[count * ChdSectorDataSize];
            try
            {
                uint lastHunk = uint.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    long sector = startSector + i;
                    uint hunkNum = (uint)(sector / framesPerHunk);
                    int frameInHunk = (int)(sector % framesPerHunk);

                    if (hunkNum != lastHunk)
                    {
                        var error = NativeMethods.chd_read(_chdHandle, hunkNum, HunkBuffer);
                        if (error != ChdError.None)
                            throw new Exception($"Failed to read CHD hunk {hunkNum}: {chd_error_string_safe(error)}");
                        lastHunk = hunkNum;
                    }

                    Marshal.Copy(HunkBuffer + (frameInHunk * ChdFrameSize), result, i * ChdSectorDataSize, ChdSectorDataSize);
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        private static int GetExtraFrames(int frames)
        {
            return ((frames + TrackPadding - 1) / TrackPadding) * TrackPadding - frames;
        }

        public long GetTrackDataStartSector(int trackIndex)
        {
            if (trackIndex < 0 || trackIndex >= Tracks.Count)
                throw new ArgumentOutOfRangeException(nameof(trackIndex));

            long logicalSector = 0;

            for (int i = 0; i < trackIndex; i++)
            {
                var track = Tracks[i];
                logicalSector += track.Frames;
            }
            logicalSector += Tracks[trackIndex].Pregap;
            return logicalSector;
        }

        public long GetTrackLogicalTotalSectors(int trackIndex)
        {
            var track = Tracks[trackIndex];
            return track.Pregap + track.Frames;
        }

        public long LogicalToPhysicalSector(long logicalSector)
        {
            long physicalSector = 0;
            long remaining = logicalSector;

            foreach (var track in Tracks)
            {
                long trackTotal = track.Pregap + track.Frames;
                if (remaining < trackTotal)
                {
                    physicalSector += remaining;
                    break;
                }
                physicalSector += trackTotal + GetExtraFrames(track.Frames);
                remaining -= trackTotal;
            }
            return physicalSector;
        }

        public long GetTrackPhysicalStartSector(int trackIndex)
        {
            if (trackIndex < 0 || trackIndex >= Tracks.Count)
                throw new ArgumentOutOfRangeException(nameof(trackIndex));

            long physicalSector = 0;
            for (int i = 0; i < trackIndex; i++)
            {
                var track = Tracks[i];
                physicalSector += track.Frames + GetExtraFrames(track.Frames);
                if (i > 0)
                {
                    physicalSector += track.Pregap;
                }
            }
            physicalSector += Tracks[trackIndex].Pregap;
            return physicalSector;
        }

        private static string chd_error_string_safe(ChdError error)
        {
            try
            {
                IntPtr ptr = NativeMethods.chd_error_string(error);
                if (ptr != IntPtr.Zero)
                    return Marshal.PtrToStringAnsi(ptr);
            } catch { }
            return error.ToString();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (HunkBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(HunkBuffer);

                if (_chdHandle != IntPtr.Zero)
                {
                    NativeMethods.chd_close(_chdHandle);
                    _chdHandle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }
    }
}
