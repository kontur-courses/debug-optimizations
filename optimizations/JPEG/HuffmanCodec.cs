using System;
using System.Collections.Generic;

namespace JPEG;

class HuffmanNode
{
    public byte? LeafLabel { get; set; }
    public int Frequency { get; set; }
    public HuffmanNode Left { get; set; }
    public HuffmanNode Right { get; set; }
}

public struct BitsWithLength : IEquatable<BitsWithLength>
{
    public int Bits { get; set; }
    public int BitsCount { get; set; }

    public bool Equals(BitsWithLength x)
    {
        return BitsCount == x.BitsCount && Bits == x.Bits;
    }

    public override bool Equals(object obj)
    {
        return obj is BitsWithLength other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ((397 * Bits) << 5) ^ (17 * BitsCount);
    }
}

class BitsBuffer
{
    private readonly List<byte> buffer;
    private BitsWithLength unfinishedBits = new BitsWithLength();

    public BitsBuffer(int capacity = 0)
    {
        buffer = capacity > 0 ? new List<byte>(capacity) : new List<byte>();
    }

    public void Add(BitsWithLength bitsWithLength)
    {
        var bitsCount = bitsWithLength.BitsCount;
        var bits = bitsWithLength.Bits;

        var neededBits = 8 - unfinishedBits.BitsCount;
        while (bitsCount >= neededBits)
        {
            bitsCount -= neededBits;
            buffer.Add((byte)((unfinishedBits.Bits << neededBits) + (bits >> bitsCount)));

            bits = bits & ((1 << bitsCount) - 1);

            unfinishedBits.Bits = 0;
            unfinishedBits.BitsCount = 0;

            neededBits = 8;
        }

        unfinishedBits.BitsCount += bitsCount;
        unfinishedBits.Bits = (unfinishedBits.Bits << bitsCount) + bits;
    }

    public byte[] ToArray(out long bitsCount)
    {
        bitsCount = buffer.Count * 8L + unfinishedBits.BitsCount;
        var result = new byte[bitsCount / 8 + (bitsCount % 8 > 0 ? 1 : 0)];
        buffer.CopyTo(result);
        if (unfinishedBits.BitsCount > 0)
            result[buffer.Count] = (byte)(unfinishedBits.Bits << (8 - unfinishedBits.BitsCount));
        return result;
    }
}

class HuffmanCodec
{
    public static byte[] Encode(IReadOnlyList<byte> data, out Dictionary<BitsWithLength, byte> decodeTable, out long bitsCount)
    {
        if (data.Count == 0)
        {
            decodeTable = new Dictionary<BitsWithLength, byte>();
            bitsCount = 0;
            return Array.Empty<byte>();
        }

        var frequences = CalcFrequences(data);
        var root = BuildHuffmanTree(frequences);

        var encodeTable = new BitsWithLength[byte.MaxValue + 1];
        FillEncodeTable(root, encodeTable);

        var bitsBuffer = new BitsBuffer(data.Count);
        for (var i = 0; i < data.Count; i++)
        {
            bitsBuffer.Add(encodeTable[data[i]]);
        }

        decodeTable = CreateDecodeTable(encodeTable);
        return bitsBuffer.ToArray(out bitsCount);
    }

    public static byte[] Decode(byte[] encodedData, Dictionary<BitsWithLength, byte> decodeTable, long bitsCount, int size)
    {
        var result = new byte[size];
        var arrayIndex = 0;

        byte decodedByte;
        var sample = new BitsWithLength { Bits = 0, BitsCount = 0 };
        for (var byteNum = 0; byteNum < encodedData.Length; byteNum++)
        {
            var b = encodedData[byteNum];
            for (var bitNum = 0; bitNum < 8 && byteNum * 8 + bitNum < bitsCount; bitNum++)
            {
                sample.Bits = (sample.Bits << 1) + ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0);
                sample.BitsCount++;

                if (decodeTable.TryGetValue(sample, out decodedByte))
                {
                    if (arrayIndex >= result.Length)
                        return result;

                    result[arrayIndex] = decodedByte;
                    arrayIndex++;
                    sample.BitsCount = 0;
                    sample.Bits = 0;
                }
            }
        }

        return result;
    }

    private static Dictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable)
    {
        var result = new Dictionary<BitsWithLength, byte>();
        for (int b = 0; b < encodeTable.Length; b++)
        {
            var bitsWithLength = encodeTable[b];
            if (bitsWithLength.BitsCount > 0)
            {
                result[bitsWithLength] = (byte)b;
            }
        }
        return result;
    }

    private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable, int bitvector = 0, int depth = 0)
    {
        if (node.LeafLabel != null)
        {
            encodeSubstitutionTable[node.LeafLabel.Value] = new BitsWithLength 
            { 
                Bits = bitvector, 
                BitsCount = depth == 0 ? 1 : depth 
            };
        }
        else
        {
            if (node.Left != null)
            {
                FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);
                FillEncodeTable(node.Right, encodeSubstitutionTable, (bitvector << 1) + 0, depth + 1);
            }
        }
    }

    private static HuffmanNode BuildHuffmanTree(int[] frequences)
    {
        var nodes = GetNodes(frequences);
        if (nodes.Count == 0)
            throw new InvalidOperationException();
        if (nodes.Count == 1)
            return nodes[0];

        while (nodes.Count > 1)
        {
            var minIndex1 = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Frequency < nodes[minIndex1].Frequency)
                    minIndex1 = i;
            }
            var firstMin = nodes[minIndex1];
            nodes.RemoveAt(minIndex1);

            var minIndex2 = 0;
            for (var j = 0; j < nodes.Count; j++)
            {
                if (nodes[j].Frequency < nodes[minIndex2].Frequency)
                    minIndex2 = j;
            }
            var secondMin = nodes[minIndex2];
            nodes.RemoveAt(minIndex2);

            nodes.Add(new HuffmanNode
            {
                Frequency = firstMin.Frequency + secondMin.Frequency,
                Left = secondMin,
                Right = firstMin
            });
        }

        return nodes[0];
    }

    private static List<HuffmanNode> GetNodes(int[] frequences)
    {
        var nodes = new List<HuffmanNode>(byte.MaxValue + 1);
        for (var i = 0; i < byte.MaxValue + 1; i++)
        {
            if (frequences[i] > 0)
            {
                nodes.Add(new HuffmanNode
                {
                    Frequency = frequences[i],
                    LeafLabel = (byte)i
                });
            }
        }
        return nodes;
    }

    private static int[] CalcFrequences(IReadOnlyList<byte> data)
    {
        var result = new int[byte.MaxValue + 1];
        for (var i = 0; i < data.Count; i++)
        {
            result[data[i]]++;
        }
        return result;
    }
}