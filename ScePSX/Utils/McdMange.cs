
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace ScePSX
{
    public class MemCardMange
    {
        public const int MaxSlot = 15;

        byte[] RawData = new byte[131072];

        public class SlotData
        {
            public byte[] Head = new byte[128];
            public byte[] Data = new byte[8192];

            public SlotTypes type;
            public int IconFrames = 0;
            public Color[] IconPalette = new Color[16];
            public Color[][] IconData = new Color[3][];

            public string ProdCode;
            public string Identifier;
            public string Region;
            public string RegionRaw;
            public int Size;
            public string Name;

            public SlotData()
            {
                IconData[0] = new Color[256];
                IconData[1] = new Color[256];
                IconData[2] = new Color[256];
            }

            public Bitmap GetIconBitmap(int idx)
            {
                int width = 16;
                int height = 16;
                Bitmap bitmap = new Bitmap(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        Color color = IconData[idx][index];
                        bitmap.SetPixel(x, y, color);
                    }
                }

                return bitmap;
            }
        }

        public SlotData[] Slots = new SlotData[MaxSlot];

        public enum SlotTypes : byte
        {
            formatted,
            initial,
            middle_link,
            end_link,
            deleted_initial,
            deleted_middle_link,
            deleted_end_link,
            corrupted
        };

        public MemCardMange()
        {
            Slots = new SlotData[MaxSlot];
            for (int i = 0; i < MaxSlot; i++)
            {
                Slots[i] = new SlotData();
            }
        }

        public MemCardMange(string fn) : this()
        {
            OpenCard(fn);
        }

        private void LoadCard()
        {
            for (int slotNumber = 0; slotNumber < MaxSlot; slotNumber++)
            {
                for (int currentByte = 0; currentByte < 128; currentByte++)
                {
                    Slots[slotNumber].Head[currentByte] = RawData[128 + (slotNumber * 128) + currentByte];
                }

                for (int currentByte = 0; currentByte < 8192; currentByte++)
                {
                    Slots[slotNumber].Data[currentByte] = RawData[8192 + (slotNumber * 8192) + currentByte];
                }
            }
        }

        private void SaveCard()
        {
            RawData = new byte[131072];

            RawData[0] = 0x4D;        //M
            RawData[1] = 0x43;        //C
            RawData[127] = 0x0E;      //XOR (precalculated)

            RawData[8064] = 0x4D;     //M
            RawData[8065] = 0x43;     //C
            RawData[8191] = 0x0E;     //XOR (precalculated)

            for (int slotNumber = 0; slotNumber < MaxSlot; slotNumber++)
            {
                for (int currentByte = 0; currentByte < 128; currentByte++)
                {
                    RawData[128 + (slotNumber * 128) + currentByte] = Slots[slotNumber].Head[currentByte];
                }

                for (int currentByte = 0; currentByte < 8192; currentByte++)
                {
                    RawData[8192 + (slotNumber * 8192) + currentByte] = Slots[slotNumber].Data[currentByte];
                }
            }

        }

        private void findBrokenLinks()
        {
            bool[] slotTouched = new bool[MaxSlot];

            for (int slotNumber = 0; slotNumber < MaxSlot; slotNumber++)
            {
                switch (Slots[slotNumber].type)
                {
                    case SlotTypes.initial:
                    case SlotTypes.deleted_initial:
                        foreach (int slot in FindSaveLinks(slotNumber))
                            slotTouched[slot] = true;
                        break;
                }
            }

            for (int slotNumber = 0; slotNumber < MaxSlot; slotNumber++)
            {
                switch (Slots[slotNumber].type)
                {
                    case SlotTypes.middle_link:
                    case SlotTypes.end_link:
                    case SlotTypes.deleted_middle_link:
                    case SlotTypes.deleted_end_link:
                        if (!slotTouched[slotNumber])
                            Slots[slotNumber].type = (int)SlotTypes.formatted;
                        break;
                }
            }
        }

        private void loadSlotTypes()
        {
            for (int slotNumber = 0; slotNumber < MaxSlot; slotNumber++)
            {
                switch (Slots[slotNumber].Head[0])
                {
                    default:
                        Slots[slotNumber].type = SlotTypes.corrupted;
                        break;

                    case 0xA0:
                        Slots[slotNumber].type = SlotTypes.formatted;
                        break;

                    case 0x51:
                        Slots[slotNumber].type = SlotTypes.initial;
                        break;

                    case 0x52:
                        Slots[slotNumber].type = SlotTypes.middle_link;
                        break;

                    case 0x53:
                        Slots[slotNumber].type = SlotTypes.end_link;
                        break;

                    case 0xA1:
                        Slots[slotNumber].type = SlotTypes.deleted_initial;
                        break;

                    case 0xA2:
                        Slots[slotNumber].type = SlotTypes.deleted_middle_link;
                        break;

                    case 0xA3:
                        Slots[slotNumber].type = SlotTypes.deleted_end_link;
                        break;
                }

            }

        }

        private void loadStringData()
        {
            byte[] tempByteArray;

            for (int slotNumber = 0; slotNumber < MaxSlot; slotNumber++)
            {
                Slots[slotNumber].ProdCode = "";
                Slots[slotNumber].Identifier = "";

                switch (Slots[slotNumber].type)
                {
                    default:
                        tempByteArray = new byte[2];
                        tempByteArray[0] = Slots[slotNumber].Head[10];
                        tempByteArray[1] = Slots[slotNumber].Head[11];

                        Slots[slotNumber].RegionRaw = Encoding.Default.GetString(tempByteArray);

                        switch (Slots[slotNumber].RegionRaw)
                        {
                            default:
                                Slots[slotNumber].Region = Slots[slotNumber].RegionRaw;
                                break;

                            case "BA":
                                Slots[slotNumber].Region = "US";
                                break;

                            case "BE":
                                Slots[slotNumber].Region = "EU";
                                break;

                            case "BI":
                                Slots[slotNumber].Region = "JP";
                                break;
                        }

                        foreach (int slot in FindSaveLinks(slotNumber))
                        {
                            if (slot != slotNumber)
                                Slots[slot].Region = Slots[slotNumber].Region;
                        }

                        tempByteArray = new byte[10];
                        for (int byteCount = 0; byteCount < 10; byteCount++)
                            tempByteArray[byteCount] = Slots[slotNumber].Head[byteCount + 12];

                        Slots[slotNumber].ProdCode = Encoding.Default.GetString(tempByteArray);
                        Slots[slotNumber].ProdCode = Slots[slotNumber].ProdCode.Replace("\0", string.Empty);

                        tempByteArray = new byte[8];
                        for (int byteCount = 0; byteCount < 8; byteCount++)
                            tempByteArray[byteCount] = Slots[slotNumber].Head[byteCount + 22];

                        Slots[slotNumber].Identifier = Encoding.Default.GetString(tempByteArray);
                        Slots[slotNumber].Identifier = Slots[slotNumber].Identifier.Replace("\0", string.Empty);

                        tempByteArray = new byte[64];
                        for (int currentByte = 0; currentByte < 64; currentByte++)
                        {
                            byte b = Slots[slotNumber].Data[currentByte + 4];
                            if ( currentByte % 2 == 0 && b == 0)
                            {
                                break;
                            }
                            tempByteArray[currentByte] = b;
                        }

                        //Shift-JIS to UTF-16
                        var encodingProvider = CodePagesEncodingProvider.Instance.GetEncoding(932);
                        if (encodingProvider != null)
                            Slots[slotNumber].Name = encodingProvider.GetString(tempByteArray).Normalize(NormalizationForm.FormKC);

                        if (Slots[slotNumber].Name == "")
                            Slots[slotNumber].Name = Encoding.Default.GetString(tempByteArray, 0, 32);
                        break;

                    case SlotTypes.middle_link:
                    case SlotTypes.deleted_middle_link:
                        Slots[slotNumber].Name = "Linked slot (middle)";
                        break;

                    case SlotTypes.end_link:
                    case SlotTypes.deleted_end_link:
                        Slots[slotNumber].Name = "Linked slot (end)";
                        break;
                }
            }
        }

        private void loadSaveSize()
        {
            for (int slotNumber = 0; slotNumber < 15; slotNumber++)
                Slots[slotNumber].Size = (Slots[slotNumber].Head[4] | (Slots[slotNumber].Head[5] << 8) | (Slots[slotNumber].Head[6] << 16)) / 1024;
        }

        public void DeleteSave(int slotNumber)
        {

            int[] saveSlots = FindSaveLinks(slotNumber);

            for (int i = 0; i < saveSlots.Length; i++)
            {
                switch (Slots[saveSlots[i]].type)
                {
                    default:
                        break;

                    case SlotTypes.initial:               //Regular save
                        Slots[saveSlots[i]].Head[0] = 0xA1;
                        break;

                    case SlotTypes.middle_link:           //Middle link
                        Slots[saveSlots[i]].Head[0] = 0xA2;
                        break;

                    case SlotTypes.end_link:              //End link
                        Slots[saveSlots[i]].Head[0] = 0xA3;
                        break;

                    case SlotTypes.deleted_initial:       //Regular deleted save
                        Slots[saveSlots[i]].Head[0] = 0x51;
                        break;

                    case SlotTypes.deleted_middle_link:   //Middle link deleted
                        Slots[saveSlots[i]].Head[0] = 0x52;
                        break;

                    case SlotTypes.deleted_end_link:      //End link deleted
                        Slots[saveSlots[i]].Head[0] = 0x53;
                        break;
                }
            }

            calculateXOR();
            loadSlotTypes();
            findBrokenLinks();
        }

        public void FormatSave(int slotNumber)
        {
            int[] saveSlots = FindSaveLinks(slotNumber);

            for (int i = 0; i < saveSlots.Length; i++)
            {
                DeleteSlot(saveSlots[i]);
            }

            calculateXOR();
            LoadData();
        }

        public int[] FindSaveLinks(int initialSlotNumber)
        {
            List<int> tempSlotList = new List<int>();
            int currentSlot = initialSlotNumber;

            for (int i = 0; i < 15; i++)
            {
                tempSlotList.Add(currentSlot);

                if (Slots[currentSlot].type == SlotTypes.corrupted)
                    break;

                if (Slots[currentSlot].Head[8] == 0xFF)
                    break;

                if (Slots[currentSlot].Head[8] > 15)
                    break;

                switch (Slots[Slots[currentSlot].Head[8]].type)
                {
                    default:
                        return tempSlotList.ToArray();

                    case SlotTypes.middle_link:
                    case SlotTypes.end_link:
                    case SlotTypes.deleted_middle_link:
                    case SlotTypes.deleted_end_link:
                        currentSlot = Slots[currentSlot].Head[8];
                        break;
                }
            }

            return tempSlotList.ToArray();
        }

        private int[] FindFreeSlots(int slotNumber, int requiredSlots)
        {
            List<int> tempSlotList = new List<int>();
            int currentSlot = 0;

            for (int i = 0; i < MaxSlot; i++)
            {
                currentSlot = (i + slotNumber) % MaxSlot;
                if (Slots[currentSlot].type == SlotTypes.formatted)
                    tempSlotList.Add(currentSlot);
                if (tempSlotList.Count == requiredSlots)
                    break;
            }

            return tempSlotList.ToArray();
        }

        public byte[] GetSaveBytes(int slotNumber)
        {
            int[] saveSlots = FindSaveLinks(slotNumber);

            byte[] saveBytes = new byte[8320 + ((saveSlots.Length - 1) * 8192)];

            for (int i = 0; i < 128; i++)
                saveBytes[i] = Slots[saveSlots[0]].Head[i];

            for (int sNumber = 0; sNumber < saveSlots.Length; sNumber++)
            {
                for (int i = 0; i < 8192; i++)
                    saveBytes[128 + (sNumber * 8192) + i] = Slots[saveSlots[sNumber]].Data[i];
            }

            return saveBytes;
        }

        public void ReplaceSaveBytes(int slotNumber, byte[] saveBytes)
        {
            int[] saveSlots = FindSaveLinks(slotNumber);

            for (int i = 0; i < 128; i++)
                Slots[saveSlots[0]].Head[i] = saveBytes[i];

            for (int i = 0; i < saveSlots.Count(); i++)
            {
                for (int byteCount = 0; byteCount < 8192; byteCount++)
                {
                    Slots[saveSlots[i]].Head[byteCount] = saveBytes[128 + (i * 8192) + byteCount];
                }
            }

            calculateXOR();

            LoadData();
        }

        public bool SetSaveBytes(int slotNumber, byte[] saveBytes)
        {
            int slotCount = (saveBytes.Length - 128) / 8192;
            int[] freeSlots = FindFreeSlots(slotNumber, slotCount);
            int numberOfBytes = slotCount * 8192;

            if (freeSlots.Length < slotCount)
                return false;

            for (int i = 0; i < 128; i++)
                Slots[freeSlots[0]].Head[i] = saveBytes[i];

            Slots[freeSlots[0]].Head[4] = (byte)(numberOfBytes & 0xFF);
            Slots[freeSlots[0]].Head[5] = (byte)((numberOfBytes & 0xFF00) >> 8);
            Slots[freeSlots[0]].Head[6] = (byte)((numberOfBytes & 0xFF0000) >> 16);

            for (int i = 0; i < slotCount; i++)
            {
                for (int byteCount = 0; byteCount < 8192; byteCount++)
                {
                    Slots[freeSlots[i]].Data[byteCount] = saveBytes[128 + (i * 8192) + byteCount];
                }
            }

            for (int i = 0; i < (freeSlots.Length - 1); i++)
            {
                Slots[freeSlots[i]].Head[0] = 0x52;
                Slots[freeSlots[i]].Head[8] = (byte)freeSlots[i + 1];
                Slots[freeSlots[i]].Head[9] = 0x00;
            }

            Slots[freeSlots[freeSlots.Length - 1]].Head[0] = 0x53;
            Slots[freeSlots[freeSlots.Length - 1]].Head[8] = 0xFF;
            Slots[freeSlots[freeSlots.Length - 1]].Head[9] = 0xFF;

            Slots[freeSlots[0]].Head[0] = 0x51;

            calculateXOR();

            LoadData();

            return true;
        }

        private void loadPalette()
        {
            int redChannel = 0;
            int greenChannel = 0;
            int blueChannel = 0;
            int colorCounter = 0;
            int blackFlag = 0;

            for (int slotNumber = 0; slotNumber < 15; slotNumber++)
            {
                colorCounter = 0;
                for (int byteCount = 0; byteCount < 32; byteCount += 2)
                {
                    redChannel = (Slots[slotNumber].Data[byteCount + 96] & 0x1F) << 3;
                    greenChannel = ((Slots[slotNumber].Data[byteCount + 97] & 0x3) << 6) | ((Slots[slotNumber].Data[byteCount + 96] & 0xE0) >> 2);
                    blueChannel = ((Slots[slotNumber].Data[byteCount + 97] & 0x7C) << 1);
                    blackFlag = (Slots[slotNumber].Data[byteCount + 97] & 0x80);

                    //Get the color value
                    if ((redChannel | greenChannel | blueChannel | blackFlag) == 0)
                        Slots[slotNumber].IconPalette[colorCounter] = Color.Transparent;
                    else
                        Slots[slotNumber].IconPalette[colorCounter] = Color.FromArgb(redChannel, greenChannel, blueChannel);
                    colorCounter++;
                }
            }
        }

        private void loadIcons()
        {
            int byteCount = 0;

            for (int slotNumber = 0; slotNumber < 15; slotNumber++)
            {
                int[] saveLinks = FindSaveLinks(slotNumber);

                for (int iconNumber = 0; iconNumber < 3; iconNumber++)
                {
                    if (Slots[slotNumber].type == SlotTypes.initial || Slots[slotNumber].type == SlotTypes.deleted_initial)
                    {
                        byteCount = 128 + (128 * iconNumber);

                        for (int y = 0; y < 16; y++)
                        {
                            for (int x = 0; x < 16; x += 2)
                            {

                                foreach (int selectedSlot in saveLinks)
                                {
                                    Slots[selectedSlot].IconData[iconNumber][x + (y * 16)] = Slots[slotNumber].IconPalette[Slots[slotNumber].Data[byteCount] & 0xF];
                                    Slots[selectedSlot].IconData[iconNumber][x + (y * 16) + 1] = Slots[slotNumber].IconPalette[Slots[slotNumber].Data[byteCount] >> 4];
                                }

                                byteCount++;
                            }
                        }
                    }
                }
            }
        }

        public byte[] GetIconBytes(int slotNumber)
        {
            byte[] iconBytes = new byte[416];

            for (int i = 0; i < 416; i++)
                iconBytes[i] = Slots[slotNumber].Data[i + 96];

            return iconBytes;
        }

        public void SetIconBytes(int slotNumber, byte[] iconBytes)
        {
            int[] saveSlots = FindSaveLinks(slotNumber);

            for (int i = 0; i < 416; i++)
                Slots[slotNumber].Data[i + 96] = iconBytes[i];

            loadPalette();
            loadIcons();
        }

        private void loadIconFrames()
        {
            for (int slotNumber = 0; slotNumber < 15; slotNumber++)
            {
                switch (Slots[slotNumber].Data[2])
                {
                    default:
                        break;

                    case 0x11:
                        Slots[slotNumber].IconFrames = 1;
                        break;

                    case 0x12:
                        Slots[slotNumber].IconFrames = 2;
                        break;

                    case 0x13:
                        Slots[slotNumber].IconFrames = 3;
                        break;
                }
            }
        }

        private void calculateXOR()
        {
            byte XORchecksum = 0;

            for (int slotNumber = 0; slotNumber < 15; slotNumber++)
            {
                XORchecksum = 0;

                for (int byteCount = 0; byteCount < 127; byteCount++)
                    XORchecksum ^= Slots[slotNumber].Head[byteCount];

                Slots[slotNumber].Head[127] = XORchecksum;
            }
        }

        public void DeleteSlot(int slotNumber)
        {
            for (int byteCount = 0; byteCount < 128; byteCount++)
                Slots[slotNumber].Head[byteCount] = 0x00;

            for (int byteCount = 0; byteCount < 8192; byteCount++)
                Slots[slotNumber].Data[byteCount] = 0x00;

            Slots[slotNumber].Head[0] = 0xA0;
            Slots[slotNumber].Head[8] = 0xFF;
            Slots[slotNumber].Head[9] = 0xFF;
        }

        public void FormatCard()
        {
            for (int slotNumber = 0; slotNumber < 15; slotNumber++)
                DeleteSlot(slotNumber);

            calculateXOR();
            loadStringData();
            loadSlotTypes();
            loadSaveSize();
            loadPalette();
            loadIcons();
            loadIconFrames();
        }

        public bool SaveSingleSave(string fileName, int slotNumber)
        {
            BinaryWriter binWriter;
            byte[] outputData = GetSaveBytes(slotNumber);

            try
            {
                binWriter = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
            } catch (Exception)
            {
                return false;
            }

            byte[] arHeader = new byte[54];
            byte[] arName = null;

            for (int byteCount = 0; byteCount < 22; byteCount++)
                arHeader[byteCount] = Slots[slotNumber].Head[byteCount + 10];

            arName = Encoding.Default.GetBytes(Slots[slotNumber].Name);

            for (int byteCount = 0; byteCount < arName.Length; byteCount++)
                arHeader[byteCount + 21] = arName[byteCount];

            binWriter.Write(arHeader);
            binWriter.Write(outputData, 128, outputData.Length - 128);

            binWriter.Close();

            return true;
        }

        public bool OpenSingleSave(string fileName, int slotNumber)
        {
            byte[] inputData;
            byte[] finalData;
            BinaryReader binReader;

            try
            {
                binReader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            } catch (Exception)
            {
                return false;
            }

            inputData = binReader.ReadBytes(123008);
            binReader.Close();


            finalData = new byte[inputData.Length + 128];
            byte[] singleSaveHeader = Encoding.Default.GetBytes(Path.GetFileName(fileName));

            finalData[0] = 0x51;

            for (int i = 0; i < 20 && i < singleSaveHeader.Length; i++)
                finalData[i + 10] = singleSaveHeader[i];

            for (int i = 0; i < inputData.Length; i++)
                finalData[i + 128] = inputData[i];

            if (SetSaveBytes(slotNumber, finalData))
                return true;
            else
                return false;
        }

        public bool SaveCard(string fileName)
        {
            BinaryWriter binWriter = null;

            try
            {
                binWriter = new BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
            } catch (Exception)
            {
                return false;
            }

            LoadCard();
            binWriter.Write(RawData);
            binWriter.Close();
            return true;
        }

        public void OpenCard(byte[] memCardData)
        {
            Array.Copy(memCardData, RawData, memCardData.Length);
            LoadCard();
            LoadData();
        }

        private void LoadData()
        {
            loadSlotTypes();
            findBrokenLinks();
            loadStringData();
            loadSaveSize();
            loadPalette();
            loadIcons();
            loadIconFrames();
        }

        public string OpenCard(string FileName)
        {
            byte[] tempData;
            int startOffset;
            BinaryReader binReader;
            long fileSize = 0;

            try
            {
                binReader = new BinaryReader(File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            } catch (Exception errorException)
            {
                return errorException.Message;
            }

            tempData = new byte[134976];

            binReader.BaseStream.Read(tempData, 0, 134976);
            fileSize = binReader.BaseStream.Length;
            binReader.Close();

            startOffset = 0;

            Array.Copy(tempData, startOffset, RawData, 0, 131072);

            LoadCard();
            LoadData();

            return null;
        }
    }
}
