using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using MiscUtil.Conversion;
using MiscUtil.IO;

namespace UnityEditor.ABR
{
    [ScriptedImporter(1, "abr")]
    public class PhotoshopBrushImporter : ScriptedImporter
    {

        public override void OnImportAsset(AssetImportContext ctx)
        {

            var converter = new BigEndianBitConverter();
            using (var fs = File.Open(ctx.assetPath, FileMode.Open))
            {
                var ebr = new EndianBinaryReader(converter, fs);

                int ver = ebr.ReadInt16();
                switch (ver)
                {
                    case 1:
                        //this.ReadVer12(ebr, ver, cancellationToken);
                        break;

                    case 2:
                        //this.ReadVer12(ebr, ver, cancellationToken);
                        break;

                    case 6:
                        this.ReadVer6(ebr, ctx);
                        break;

                    default:
                        Debug.LogError("Unsupported file version");
                        break;
                }
            }
        }
        private void ReadVer6(EndianBinaryReader ebr, AssetImportContext ctx)
        {
            int width = 0;
            int height = 0;
            int num3 = ebr.ReadInt16();
            ebr.ReadBytes(8);
            int num5 = ebr.ReadInt32() + 12;
            int index = 0;
            while (ebr.BaseStream.Position < (num5 - 1))
            {

                int num6 = ebr.ReadInt32();
                int num7 = num6;
                while ((num7 % 4) != 0)
                    num7++;

                int num8 = num7 - num6;
                ebr.ReadString();

                switch (num3)
                {
                    case 1:
                        ebr.ReadInt16();
                        ebr.ReadInt16();
                        ebr.ReadInt16();
                        ebr.ReadInt16();
                        ebr.ReadInt16();
                        int num9 = ebr.ReadInt32();
                        int num10 = ebr.ReadInt32();
                        int num11 = ebr.ReadInt32();
                        width = ebr.ReadInt32() - num10;
                        height = num11 - num9;
                        break;
                    case 2:
                        ebr.ReadBytes(0x108);
                        int num13 = ebr.ReadInt32();
                        int num14 = ebr.ReadInt32();
                        int num15 = ebr.ReadInt32();
                        width = ebr.ReadInt32() - num14;
                        height = num15 - num13;
                        break;
                }

                ebr.ReadInt16();

                byte[] buffer;
                if (ebr.ReadByte() == 0)
                {
                    buffer = ebr.ReadBytes(width * height);
                }
                else
                {
                    int num18 = 0;
                    for (int j = 0; j < height; j++)
                        num18 += ebr.ReadInt16();

                    byte[] imgdata = ebr.ReadBytes(num18);
                    buffer = Unpack(imgdata);
                }

                Texture2D tex = CreateImage(width, height, buffer);
                tex.alphaIsTransparency = true;
                string name = $"{System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath)}_{index}";
                tex.name = name;
                ctx.AddObjectToAsset(name, tex, tex);

                index++;

                switch (num3)
                {
                    case 1:
                        ebr.ReadBytes(num8);
                        continue;
                    case 2:
                        ebr.ReadBytes(8);
                        ebr.ReadBytes(num8);
                        break;
                }
            }
        }

        private Texture2D CreateImage(int width, int height, byte[] buffer)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.Alpha8, false);

            int pixelIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, new Color32(0, 0, 0, buffer[pixelIndex++]));
                }
            }

            tex.Apply();
            return tex;

        }
        private byte[] Unpack(byte[] imgdata)
        {
            using (var input = new MemoryStream(imgdata))
            using (var output = new MemoryStream(imgdata.Length))
            {
                var reader = new BinaryReader(input);
                var writer = new BinaryWriter(output);

                var length = imgdata.Length - sizeof(byte);
                while (input.Position < length)
                {
                    sbyte count = reader.ReadSByte();
                    if (count >= 0)
                    {
                        writer.Write(reader.ReadBytes(count + 1));
                    }
                    else
                    {
                        byte value = reader.ReadByte();
                        while (count++ <= 0)
                            writer.Write(value);
                    }
                }
                return output.ToArray();
            }
        }
    }
}
