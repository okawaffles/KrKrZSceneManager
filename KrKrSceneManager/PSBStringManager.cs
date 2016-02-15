﻿using System;
using System.IO;
using System.Text;

namespace KrKrSceneManager
{  
    public class PSBStringManager
    {
        private int DefaultOffsetSize;
        private int StringTable;
        private int OffsetTable;
        private int StrCount;
        private string Status = "Not Open";
        private byte[] Source = new byte[0];
        private byte[] sufix = new byte[0];
        private int TablePrefixSize = 0;

        //settings
        public bool CompressPackget = false;
        public int CompressionLevel = 9;
        public string[] Strings = new string[0];
        public bool ResizeOffsets = true;

        public bool Initialized { get; private set; }

        public byte[] Export()
        {
            if (!Initialized)
                throw new Exception("You need import a scene before export.");
            byte[] Script = new byte[OffsetTable + TablePrefixSize];
            for (int pos = 0; pos < Script.Length; pos++)
            {
                Status = "Copying Script...";
                Script[pos] = Source[pos];
            }
            int OffsetSize = DefaultOffsetSize;
            if (ResizeOffsets)
            {
                Script[Script.Length - 1] = ConvertSize(4);
                OffsetSize = 4;
                writeOffset(Script, 0x14, OffsetTable + TablePrefixSize + (StrCount * OffsetSize), OffsetSize);
            }

            byte[] Offsets = new byte[StrCount * OffsetSize];
            byte[] strings = new byte[0];
            int diff = 0;
            byte[] tmp;
            for (int pos = diff; pos < Strings.Length; pos++)
            {
                Status = "Compiling strings... (" + (pos * 100) / Strings.Length + "%)";
                byte[] hex = Tools.U8StringToByte(Strings[pos]);
                tmp = new byte[strings.Length + hex.Length + 1];
                strings.CopyTo(tmp, 0);
                tmp[strings.Length] = 0x00;
                int offset = strings.Length;
                hex.CopyTo(tmp, offset);
                strings = tmp;
                Offsets = writeOffset(Offsets, pos * OffsetSize, offset, OffsetSize);
            }
            Status = "Additing Others Resouces...";
            tmp = new byte[strings.Length + sufix.Length];
            strings.CopyTo(tmp, 0);
            for (int i = strings.Length; (i - strings.Length) < sufix.Length; i++)
            {
                tmp[i] = sufix[i - strings.Length];
            }
            strings = tmp;
            Status = "Generating new scene...";
            byte[] temp = new byte[Script.Length + Offsets.Length + strings.Length];
            Script.CopyTo(temp, 0);
            Offsets.CopyTo(temp, Script.Length);
            strings.CopyTo(temp, Script.Length + Offsets.Length);
            Script = temp;
            /* Fix All Offsets - In Next Commit
            int ResPos = GetOffset(Source, 0x1C, 4, false);
            int RestCount = GetOffset(Source, ResPos + 1, ConvertSize(Source[ResPos]), false);
            int ResOffSize = ConvertSize(Source[ResPos + 1 + ConvertSize(ResPos)]) * ResPos;

            int DibPos = GetOffset(Source, 0x18, 4, false);
            int DibOffT = ConvertSize(Source[DibPos]);
            int DibCount = GetOffset(Source, DibPos + 1, DibOffT, false);
            int DibSize = ConvertSize(Source[DibPos + 1 + DibOffT]) * DibCount;

            int ResTablePos = DibSize + DibOffT + 1
            */
            return CompressPackget ? MakeMDF(Script) : Script;
        }

        internal byte[] MakeMDF(byte[] psb) {
            byte[] CompressedScript;
            Tools.CompressData(psb, CompressionLevel, out CompressedScript);
            byte[] RetData = new byte[8 + CompressedScript.Length];
            (new byte[] { 0x6D, 0x64, 0x66, 0x00 }).CopyTo(RetData, 0);
            genOffset(4, psb.Length).CopyTo(RetData, 4);
            CompressedScript.CopyTo(RetData, 8);
            return RetData;
        }
        #region res
        internal byte[] genOffset(int size, int Value)
        {
            string[] result = new string[0];
            for (int i = 0; i < size; i++)
            {
                string[] temp = new string[result.Length + 1];
                result.CopyTo(temp, 0);
                temp[result.Length] = "00";
                result = temp;
            }
            string var = Tools.IntToHex(Value);
            if (var.Length % 2 != 0)
            {
                var = 0 + var;
            }
            string[] hex = new string[var.Length / 2];
            int tmp = 0;
            for (int i = var.Length - 2; i > -2; i -= 2)
            {
                hex[tmp] = var.Substring(i, 2);
                tmp++;
            }
            tmp = 0;
            for (int i = 0; i < size; i++)
            {
                if (tmp < hex.Length)
                {
                    result[i] = hex[tmp];
                }
                else
                {
                    result[i] = "00";
                }
                tmp++;
            }
            return Tools.StringToByteArray(result);
        }
        internal byte[] writeOffset(byte[] offsets, int position, int Value, int OffsetSize)
        {
            byte[] result = offsets;
            byte[] var = Tools.IntToByte(Value);
            if (var.Length > OffsetSize)
            {
                throw new Exception("Edited Strings are too big.");
            }
            byte[] hex = new byte[var.Length];
            int tmp = 0;
            for (int i = var.Length - 1; i >= 0; i--)
            {
                hex[tmp] = var[i];
                tmp++;
            }
            tmp = 0;

            for (int i = position; i < (position + OffsetSize); i++)
            {
                if (tmp < hex.Length)
                {
                    result[i] = hex[tmp];
                }
                else
                {
                    result[i] = 0x00;
                }
                tmp++;
            }
            return result;
        }

        private int GetOffsetSize(byte[] file)
        {
            int pos = GetOffset(file, 0x10, 4, false);
            int FirstSize = ConvertSize(file[pos++]);
            return ConvertSize(file[FirstSize+pos]);
        }
        private int GetStrCount(byte[] file)
        {
            int pos = GetOffset(file, 0x10, 4, false);
            int Size = ConvertSize(file[pos++]);
            return GetOffset(file, pos, Size, false);
        }
        private int GetPrefixSize(byte[] file)
        {
            return ConvertSize(file[GetOffset(file, 0x10, 4, false)])+2;
        }
        #endregion
        internal byte ConvertSize(int s)
        {
            switch (s)
            {
                case 1:
                    return 0xD;
                case 2:
                    return 0xE;
                case 3:
                    return 0xF;
                case 4:
                    return 0x10;                    
            }
            throw new Exception("Unknow Offset Size");
        }
        internal int ConvertSize(byte b) {
            switch (b) {
                case 0xD:
                    return 1;
                case 0xE:
                    return 2;
                case 0xF:
                    return 3;
                case 0x10:
                    return 4;
            }
            throw new Exception("Unknow Offset Size");
        }
        internal string getRange(byte[] file, int pos, int length)
        {
            byte[] rest = new byte[length];
            for (int i = 0; i < length; i++)
            {
                rest[i] = file[pos + i];
            }
            return Tools.ByteArrayToString(rest).Replace("-", "");
        }
        internal byte[] GetMDF(byte[] mdf) {
            object tmp = new byte[mdf.Length - 8];
            for (int i = 8; i < mdf.Length; i++)
                ((byte[])tmp)[i - 8] = mdf[i];
            byte[] DecompressedMDF;
            Tools.DecompressData((byte[])tmp, out DecompressedMDF);
            if (GetOffset(mdf, 4, 4, false) != DecompressedMDF.Length)
                throw new Exception("Corrupted MDF Header or Zlib Data");
            return DecompressedMDF;
        }
        public string[] Import(byte[] Packget)
        {            
            if (getRange(Packget, 0, 4) == "6D646600")
                Packget = GetMDF(Packget);
            if (getRange(Packget, 0, 3) != "505342")
                throw new Exception("Invalid KrKrZ Scene binary");
            Source = Packget;
            Status = "Reading Header...";
            OffsetTable = GetOffset(Packget, 16, 4, false);
            StringTable = GetOffset(Packget, 20, 4, false);
            DefaultOffsetSize = GetOffsetSize(Packget);
            TablePrefixSize = GetPrefixSize(Packget);
            StrCount = GetStrCount(Packget);
            Strings = new string[StrCount];
            for (int str = -1, pos = OffsetTable + TablePrefixSize; pos < StringTable; pos += DefaultOffsetSize)
            {
                str++;
                Status = "Importing Strings... (" + (str * 100) / StrCount + "%)";
                int index = GetOffset(Packget, pos, DefaultOffsetSize, false) + StringTable;
                if (Packget[index] == 0x00)
                    Strings[str] = string.Empty;
                else
                    Strings[str] = GetString(Packget, index);

                if (pos + DefaultOffsetSize >= StringTable) //if the for loop ends now
                {//get end of file
                    int Size = Encoding.UTF8.GetBytes(Strings[str]).Length+1;
                    if (index + Size <= Packget.Length)
                    {
                        sufix = new byte[Packget.Length - (index+Size)];
                        for (int i = index + Size, b = 0; i < Packget.Length; i++, b++)
                            sufix[b] = Packget[i];
                    }
                }
            }
            Status = "Imported";
            Initialized = true;
            return Strings;
        }

        internal int elevate(int ValueToElevate, int ElevateTimes) {
            if (ElevateTimes == 0)
                return 0;
            int elevate = 1;
            int value = ValueToElevate;
            while (elevate < ElevateTimes)
            {
                value *= ValueToElevate;
                elevate++;
            }
            return value;
        }

        internal bool EqualsAt(byte[] OriginalData, byte[] DataToCompare, int PositionToStartCompare)
        {
            if (PositionToStartCompare + DataToCompare.Length > OriginalData.Length)
                return false;
            for (int pos = 0; pos < DataToCompare.Length; pos++)
            {
                if (OriginalData[PositionToStartCompare + pos] != DataToCompare[pos])
                    return false;
            }
            return true;
        }

        internal byte[] genOffsetTable(int[] offsets, int Count, int size)
        {
            byte[] table = new byte[size * Count];
            for (int i = 0; i < Count; i++)
            {
                byte[] offset = genOffset(size, offsets[i]);
                offset.CopyTo(table, i*size);
            }
            return table;
        }

        public string GetStatus()
        {
            return Status;
        }

        private string GetString(byte[] scene, int pos)
        {
            string hex = "";
            for (int i = pos; scene[i] != 0x00 && i + 1 < scene.Length; i++)
                hex += scene[i].ToString("x").ToUpper() + "-";
            hex = hex.Substring(0, hex.Length - 1);
            return Tools.U8HexToString(hex.Split('-')).Replace("\n", "\\n");
        }

        internal int GetOffset(byte[] file, int index, int OffsetSize, bool reverse)
        {
            if (reverse)
            {
                string hex = "";
                for (int i = index; i < index + OffsetSize; i++) { 
                    string var = file[i + index].ToString("x").ToUpper();
                if (var.Length % 2 != 0)
                {
                    var = 0 + var;
                }
                hex += var;
            }
                return Tools.HexToInt(hex);
            }
            else
            {
                string hex = "";
                for (int i = (index + OffsetSize - 1); i > (index - 1); i--)
                {
                    string var = file[i].ToString("x").ToUpper();
                    if (var.Length % 2 != 0)
                    {
                        var = 0 + var;
                    }
                    hex += var;
                }
                return Tools.HexToInt(hex);
            }
        }
    }
    internal class Tools
    {
        internal static void CompressData(byte[] inData, int compression, out byte[] outData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, compression))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                CopyStream(inMemoryStream, outZStream);
                outZStream.Finish();
                outData = outMemoryStream.ToArray();
            }
        }
        internal static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
        internal static void DecompressData(byte[] inData, out byte[] outData)
        {
            try
            {
                using (Stream inMemoryStream = new MemoryStream(inData))
                using (ZInputStream outZStream = new ZInputStream(inMemoryStream))
                {
                    MemoryStream outMemoryStream = new MemoryStream();
                    CopyStream(outZStream, outMemoryStream);
                    outData = outMemoryStream.ToArray();
                }
            }
            catch
            {
                outData = new byte[0];
            }
        }

        internal static void DecompressData(byte[] inData, int OutSize, out byte[] outData)
        {
            outData = new byte[OutSize];
            try
            {
                using (Stream inMemoryStream = new MemoryStream(inData))
                using (ZInputStream outZStream = new ZInputStream(inMemoryStream))
                {
                    int leng = (int)outZStream.Length;
                    for (int i = 0; i < outData.Length; i++)
                        outData[i] = (byte)outZStream.ReadByte();
                }
            }
            catch
            {
                outData = new byte[0];
            }
        }
        public static string IntToHex(int val)
        {
            return val.ToString("X");
        }
        public static byte[] IntToByte(int val)
        {
            string var = IntToHex(val);
            if (var.Length % 2 != 0)
            {
                var = 0 + var;
            }
            return StringToByteArray(var);
        }
        public static string StringToHex(string _in)
        {
            string input = _in;
            char[] values = input.ToCharArray();
            string r = "";
            foreach (char letter in values)
            {
                int value = Convert.ToInt32(letter);
                string hexOutput = String.Format("{0:X}", value);
                if (value > 255)
                    return UnicodeStringToHex(input);
                r += value + " ";
            }
            string[] bytes = r.Split(' ');
            byte[] b = new byte[bytes.Length - 1];
            int index = 0;
            foreach (string val in bytes)
            {
                if (index == bytes.Length - 1)
                    break;
                if (int.Parse(val) > byte.MaxValue)
                {
                    b[index] = byte.Parse("0");
                }
                else
                    b[index] = byte.Parse(val);
                index++;
            }
            r = ByteArrayToString(b);
            return r.Replace("-", @" ");
        }
        public static string UnicodeStringToHex(string _in)
        {
            string input = _in;
            char[] values = Encoding.Unicode.GetChars(Encoding.Unicode.GetBytes(input.ToCharArray()));
            string r = "";
            foreach (char letter in values)
            {
                int value = Convert.ToInt32(letter);
                string hexOutput = String.Format("{0:X}", value);
                r += value + " ";
            }
            UnicodeEncoding unicode = new UnicodeEncoding();
            byte[] b = unicode.GetBytes(input);
            r = ByteArrayToString(b);
            return r.Replace("-", @" ");

        }
        public static string U8HexToString(string[] hex)
        {
            byte[] str = StringToByteArray(hex);
            UTF8Encoding encoder = new UTF8Encoding();
            return encoder.GetString(str);
        }
        public static string[] U8StringToHex(string text)
        {
            UTF8Encoding encoder = new UTF8Encoding();
            byte[] cnt = encoder.GetBytes(text.ToCharArray());
            return ByteArrayToString(cnt).Split('-');
        }

        public static byte[] U8StringToByte(string text)
        {
            UTF8Encoding encoder = new UTF8Encoding();
            return encoder.GetBytes(text.ToCharArray());
        }

        public static byte[] StringToByteArray(string hex)
        {
            try
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
            catch { Console.Write("Invalid format file!"); return new byte[0]; }
        }
        public static byte[] StringToByteArray(string[] hex)
        {
            try
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars];
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] = Convert.ToByte(hex[i], 16);
                return bytes;
            }
            catch { Console.Write("Invalid format file!"); return new byte[0]; }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex;
        }

        public static int HexToInt(string hex)
        {
            int num = Int32.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return num;
        }

        public static string HexToString(string hex)
        {
            string[] hexValuesSplit = hex.Split(' ');
            string returnvar = "";
            foreach (string hexs in hexValuesSplit)
            {
                int value = Convert.ToInt32(hexs, 16);
                char charValue = (char)value;
                returnvar += charValue;
            }
            return returnvar;
        }

        public static string UnicodeHexToUnicodeString(string hex)
        {
            string hexString = hex.Replace(@" ", "");
            int length = hexString.Length;
            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return Encoding.Unicode.GetString(bytes);
        }

    }
}