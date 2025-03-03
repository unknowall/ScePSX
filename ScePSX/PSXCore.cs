using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SYSLIB0011

namespace ScePSX
{
    public class PSXCore : ICoreHandler
    {
        const int PSX_MHZ = 33868800;
        public const int CYCLES_PER_FRAME = PSX_MHZ / 60;
        public int SYNC_CYCLES = 110;
        public int MIPS_UNDERCLOCK = 1;
        public int SYNC_LOOPS;
        public int SYNC_CYCLES_IDLE = 15;

        private Task MainTask;

        public BUS PsxBus;

        public string DiskID = "";
        public bool Pauseing, Pauseed, Running;

        private IAudioHandler _Audio;
        private IRenderHandler _IRender;

        public struct AddrItem
        {
            public UInt32 Address;
            public UInt32 Value;
            public Byte Width;
        }
        public struct CheatCode
        {
            public string Name;
            public List<AddrItem> Item;
            public bool Active;
        }
        public List<CheatCode> cheatCodes = new List<CheatCode> { };

        public PSXCore(IRenderHandler render, IAudioHandler audio, string RomFile, string BiosFile)
        {
            SYNC_LOOPS = (CYCLES_PER_FRAME / (SYNC_CYCLES * MIPS_UNDERCLOCK)) + 1;

            _Audio = audio;
            _IRender = render;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"ScePSX Booting...");
            Console.ResetColor();

            PsxBus = new BUS(this, BiosFile, RomFile);

            DiskID = PsxBus.DiskID;

            if (DiskID == "")
            {
                Console.WriteLine($"ScePSX Boot Fail...");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"ScePSX Running...");
            Console.ResetColor();
        }

        public void Start()
        {
            if (DiskID == "")
                return;

            if (MainTask == null && !Running && PsxBus != null)
            {
                Running = true;
                Pauseing = false;
                MainTask = Task.Factory.StartNew(PSX_EXECUTE, TaskCreationOptions.LongRunning);
            }
        }

        public void Pause()
        {
            Pauseing = !Pauseing;
        }

        public void Stop()
        {
            if (Running)
            {
                Running = false;
                Pauseing = false;
                MainTask.Wait();
            }
        }

        public void LoadCheats()
        {
            cheatCodes.Clear();
            string fn = "./Cheats/" + DiskID + ".txt";
            if (!File.Exists(fn))
                return;
            cheatCodes = ParseTextToCheatCodeList(fn);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[CHEAT] {cheatCodes.Count} Codes Loaded");
            foreach (var code in cheatCodes)
            {
                if (code.Active)
                    Console.WriteLine($"    {code.Name} [Active]");
                else
                    Console.WriteLine($"    {code.Name} [Non Active]");
            }
            Console.ResetColor();
        }

        public static List<CheatCode> ParseTextToCheatCodeList(string fn, bool isfile = true)
        {
            string input;

            if (isfile)
                input = File.ReadAllText(fn);
            else
                input = fn;

            List<CheatCode> result = new List<CheatCode>();
            string currentSection = null;
            bool isActive = false;
            List<AddrItem> currentItems = null;

            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (currentSection != null)
                    {
                        result.Add(new CheatCode { Name = currentSection, Item = currentItems, Active = isActive });
                    }
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    isActive = true;
                    currentItems = new List<AddrItem>();
                } else if (line.StartsWith("Active", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(line, @"Active\s*=\s*(true|false|0|1)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (match.Groups[1].Value == "true" || match.Groups[1].Value == "1")
                            isActive = true;
                        else
                            isActive = false;
                    }
                } else
                {
                    var match = Regex.Match(line, @"^([0-9A-F]{8})\s+([0-9A-F]{1,8})$", RegexOptions.IgnoreCase);
                    if (match.Success && currentSection != null)
                    {
                        UInt32 address = Convert.ToUInt32(match.Groups[1].Value, 16);
                        UInt32 value = Convert.ToUInt32(match.Groups[2].Value, 16);
                        //byte width = value <= 0xFF ? (byte)1 : value <= 0xFFFF ? (byte)2 : (byte)4;
                        byte width = match.Groups[2].Value.Length <= 2 ? (byte)1 : match.Groups[2].Value.Length <= 4 ? (byte)2 : (byte)4;

                        currentItems.Add(new AddrItem { Address = address, Value = value, Width = width });
                    }
                }
            }
            if (currentSection != null)
            {
                result.Add(new CheatCode { Name = currentSection, Item = currentItems, Active = isActive });
            }
            return result;
        }

        public void LoadState(string Fix = "")
        {
            if (!Running)
                return;

            string fn = "./SaveState/" + DiskID + "_Save" + Fix + ".dat";
            if (!File.Exists(fn))
                return;

            Pauseing = true;
            while (!Pauseed)
            {
                Thread.Sleep(10);
                Pauseing = true;
            };

            PsxBus = StateFromFile<BUS>(fn);

            PsxBus.DeSerializable(this);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("State LOADED.");
            Console.ResetColor();

            Pauseing = false;
        }

        public void SaveState(string Fix = "")
        {
            if (!Running)
                return;

            Pauseing = true;
            while (!Pauseed)
            {
                Thread.Sleep(10);
                Pauseing = true;
            };

            string fn = "./SaveState/" + DiskID + "_Save" + Fix + ".dat";

            PsxBus.ReadySerializable();

            StateToFile(PsxBus, fn);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("State SAVEED.");
            Console.ResetColor();

            Pauseing = false;
        }

        private BUS StateFromFile<BUS>(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                gzipStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                BinaryFormatter formatter = new BinaryFormatter();
                return (BUS)formatter.Deserialize(memoryStream);
            }
        }

        private void StateToFile<BUS>(BUS obj, string filePath)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, obj);
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
                {
                    memoryStream.Position = 0;
                    memoryStream.CopyTo(gzipStream);
                }
            }
        }

        private unsafe void ApplyCheats()
        {
            foreach (var code in cheatCodes)
            {
                if (!code.Active)
                    continue;

                foreach (var item in code.Item)
                {
                    uint addr = PsxBus.GetMask(item.Address);
                    switch (item.Width)
                    {
                        case 1:
                            PsxBus.write(addr & 0x1F_FFFF, (byte)item.Value, PsxBus.ramPtr);
                            break;
                        case 2:
                            PsxBus.write(addr & 0x1F_FFFF, (ushort)item.Value, PsxBus.ramPtr);
                            break;
                        case 4:
                            PsxBus.write(addr & 0x1F_FFFF, item.Value, PsxBus.ramPtr);
                            break;
                    }
                }
            }
        }

        private void PSX_EXECUTE()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            Stopwatch s0 = new Stopwatch();
            try
            {
                while (Running)
                {
                    if (Pauseing)
                    {
                        Pauseed = true;
                        continue;
                    }
                    Pauseed = false;

                    s0.Restart();

                    for (int i = 0; i < SYNC_LOOPS; i++)
                    {
                        for (int j = 0; j < SYNC_CYCLES; j++)
                        {
                            PsxBus.cpu.tick();
                        }
                        PsxBus.tick(SYNC_CYCLES * MIPS_UNDERCLOCK);
                        PsxBus.cpu.handleInterrupts();
                    }

                    ApplyCheats();

                    if (SYNC_CYCLES_IDLE > 0)
                        Thread.Sleep(Math.Max((int)(SYNC_CYCLES_IDLE - s0.ElapsedMilliseconds), 0));
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Button(Controller.InputAction button, bool Down = false, int conidx = 0)
        {
            if (conidx == 0)
                PsxBus.controller1.Button(button, Down);
            if (conidx == 1)
                PsxBus.controller2.Button(button, Down);
        }

        public void AnalogAxis(float lx, float ly, float rx, float ry, int conidx = 0)
        {
            if (conidx == 0)
                PsxBus.controller1.AnalogAxis(lx, ly, rx, ry);
            if (conidx == 1)
                PsxBus.controller2.AnalogAxis(lx, ly, rx, ry);
        }

        void ICoreHandler.SamplesReady(byte[] samples)
        {
            _Audio.PlaySamples(samples);
        }

        void ICoreHandler.FrameReady(int[] pixels, int width, int height)
        {
            _IRender.RenderFrame(pixels, width, height);
        }
    }

}
