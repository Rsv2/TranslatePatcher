using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Shapes;

/// <summary>
/// Represents RGSSAD format used in RPG Maker VX Ace.
/// </summary>
public class RGSSADv3 : RGSSAD
{
    public uint MainKey { get; set; }

    public RGSSADv3(string filePath) : base(filePath)
    {
        int version = GetVersion();

        if (version != Constants.RGASSDv3)
        {
            throw new InvalidArchiveException("Archive is in invalid format.");
        }

        ReadRGSSAD();
    }

    /// <summary>
    /// Reads the contents of RGSSAD archive and populates ArchivedFiles property.
    /// </summary>
    private void ReadRGSSAD()
    {
        BinaryReader.BaseStream.Seek(8, SeekOrigin.Begin);
        uint key = (uint)BinaryReader.ReadInt32();
        key *= 9;
        key += 3;
        MainKey = key;

        ArchivedFiles = new List<ArchivedFile>();

        while (true)
        {
            ArchivedFile archivedFile = new ArchivedFile();
            archivedFile.Offset = DecryptInteger(BinaryReader.ReadInt32(), key);
            archivedFile.Size = DecryptInteger(BinaryReader.ReadInt32(), key);
            archivedFile.Key = (uint)DecryptInteger(BinaryReader.ReadInt32(), key);

            int length = DecryptInteger(BinaryReader.ReadInt32(), key);

            if (archivedFile.Offset == 0)
            {
                break;
            }

            archivedFile.BName = BinaryReader.ReadBytes(length);
            archivedFile.Name = DecryptFilename(archivedFile.BName, key);
            if (ViewModel.Paths.Contains(archivedFile.Name))
            {
                archivedFile.Data = ReadData(archivedFile.Name, archivedFile.Key);
                archivedFile.Size = archivedFile.Data.Length;
            }
            else
            {
                archivedFile.Data = ReadFromBase((int)archivedFile.Offset, archivedFile.Size);
            }
            ArchivedFiles.Add(archivedFile);
        }
        int offset = (int)ArchivedFiles[0].Offset;
        for (int i = 1; i < ArchivedFiles.Count; i++)
        {
            offset += (int)ArchivedFiles[i - 1].Size;
            ArchivedFiles[i].Offset = offset;
        }
        List<byte> Output = new List<byte>(ViewModel.StartCodeTable);
        for (int i = 0; i < ArchivedFiles.Count; i++)
        {
            Output.AddRange(BitConverter.GetBytes(EnryptInteger((int)ArchivedFiles[i].Offset, MainKey)));
            Output.AddRange(BitConverter.GetBytes(EnryptInteger(ArchivedFiles[i].Size, MainKey)));
            Output.AddRange(BitConverter.GetBytes(EnryptInteger((int)ArchivedFiles[i].Key, MainKey)));
            Output.AddRange(BitConverter.GetBytes(EnryptInteger(ArchivedFiles[i].BName.Length, MainKey)));
            Output.AddRange(ArchivedFiles[i].BName);
        }
        Output.AddRange(ViewModel.MidCodeTable);
        for (int i = 0; i < ArchivedFiles.Count; i++) Output.AddRange(ArchivedFiles[i].Data);
        BinaryReader.Close();
        File.WriteAllBytes($"{FilePath}", Output.ToArray());
    }

    private byte[] ReadFromBase(int id, int length)
    {
        List<byte> bytes = new();
        for (int i = id; i < id + length; i++)
        {
            bytes.Add(FullData[i]);
        }
        return bytes.ToArray();
    }

    private byte[] ReadData(string path, uint key)
    {
        StreamResourceInfo res = Application.GetResourceStream(new Uri($"Game\\{path}", UriKind.Relative));
        using MemoryStream memoryStream = new();
        res.Stream.CopyTo(memoryStream);
        return EncriptFileData(memoryStream.ToArray(), key);
    }

    private byte[] EnryptFilename(string decryptedName, uint key)
    {
        byte[] encryptedName = Encoding.UTF8.GetBytes(decryptedName);

        byte[] keyBytes = BitConverter.GetBytes(key);

        int j = 0;
        for (int i = 0; i <= decryptedName.Length - 1; i++)
        {
            if (j == 4)
                j = 0;
            encryptedName[i] = (byte)(encryptedName[i] ^ keyBytes[j]);
            j += 1;
        }
        return encryptedName;
    }

    private int EnryptInteger(int value, uint key)
    {
        long result = value ^ key;
        return (int)result;
    }


    /// <summary>
    /// Decrypts integer from given value.
    /// </summary>
    /// <param name="value">Encrypted value</param>
    /// <param name="key">Key</param>
    /// <returns>Decrypted integer</returns>
    private int DecryptInteger(int value, uint key)
    {
        long result = value ^ key;
        return (int)result;
    }

    /// <summary>
    /// Decrypts file name from given bytes using given key.
    /// </summary>
    /// <param name="encryptedName">Encrypted filename</param>
    /// <param name="key">Key</param>
    /// <returns>Decrypted filename</returns>
    private string DecryptFilename(byte[] encryptedName, uint key)
    {
        byte[] decryptedName = new byte[encryptedName.Length];

        byte[] keyBytes = BitConverter.GetBytes(key);

        int j = 0;
        for (int i = 0; i <= encryptedName.Length - 1; i++)
        {
            if (j == 4)
                j = 0;
            decryptedName[i] = (byte)(encryptedName[i] ^ keyBytes[j]);
            j += 1;
        }

        return Encoding.UTF8.GetString(decryptedName);
    }
}
