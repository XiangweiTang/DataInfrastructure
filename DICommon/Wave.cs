﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DICommon
{
    public class Wave
    {
        public short WaveType { get; private set; } = 0;
        public short NumChannels { get; private set; } = 0;
        public int SampleRate { get; private set; } = 1;
        public int ByteRate { get; private set; } = 1;
        public short BitsPerSample { get; private set; } = 1;
        public short BlockAlign { get; private set; } = 1;
        public double AudioLength { get; private set; } = 0;
        public double RMS
        {
            get
            {
                Sanity.Requires(IsDeep, $"Please deep parse the wave file.");
                return _RMS;
            }
        }
        private double _RMS = 0;

        private const string RIFF = "RIFF";
        private const string WAVE = "WAVE";
        private const string FORMAT = "fmt ";
        private const string DATA = "data";
        // This is the number of sample, not number of bytes.
        private const int BUFFER_SIZE = 10_000;

        private bool IsDeep = false;
        public WaveChunk FormatChunk { get; private set; }= new WaveChunk { Name = "", Length = -1, Offset = -1 };
        public WaveChunk DataChunk { get; private set; } = new WaveChunk { Name = "", Length = -1, Offset = -1 };
        public List<WaveChunk> ChunkList { get; private set; } = new List<WaveChunk>();
        public Wave() { }
        public void ShallowParse(string filePath)
        {
            using(Stream fs=new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                ShallowParse(fs);
            }
        }
        public void ShallowParse(Stream fs)
        {
            IsDeep = false;
            ParseWave(fs);
        }
        public void DeepParse(string filePath)
        {
            using(Stream fs=new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                DeepParse(fs);
            }
        }

        public void DeepParse(Stream fs)
        {
            IsDeep = true;
            ParseWave(fs);
        }
        private void ParseWave(Stream fs)
        {
            Sanity.Requires(fs.Length >= 44, "Invalid wave file, size too small.");
            Sanity.Requires(fs.Length <= int.MaxValue, "Invalid wave file, size too big.");
            
            string riff = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            Sanity.Requires(riff == RIFF, "Invalid wave file, broken RIFF header.");
            
            int length = fs.ReadIntFromFileStream();
            Sanity.Requires(length + fs.Position == fs.Length, "Invalid wave file, shorter than expected.");

            string wave = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            Sanity.Requires(wave == WAVE, "Invalid wave file, broken WAVE header.");

            ParseRecursively(fs);

            PostCheck(fs);
        }

        private void ParseRecursively(Stream fs)
        {
            if (fs.Position == fs.Length)
                return;
            
            Sanity.Requires(fs.Position + 8 <= fs.Length, "Invalid wave file, shorter than expected.");
            string chunkName = fs.ReadStringFromFileStream(Encoding.ASCII, 4);
            int chunkSize = fs.ReadIntFromFileStream();
            int chunkOffset = (int)fs.Position;
            Sanity.Requires(chunkOffset + chunkSize <= fs.Length, $"Invalid wave file, shorter than expected in {chunkName}.");
            WaveChunk chunk = new WaveChunk
            {
                Name = chunkName,
                Offset = chunkOffset,
                Length = chunkSize
            };
            switch (chunk.Name)
            {
                case FORMAT:
                    Sanity.Requires(FormatChunk.Name == "", "Invalid wave file, more than one format chunk.");
                    FormatChunk = chunk;
                    break;
                case DATA:
                    Sanity.Requires(DataChunk.Name == "", "Invalid wave file, more than one data chunk.");
                    DataChunk = chunk;
                    break;
                default:
                    break;
            }
            ChunkList.Add(chunk);

            fs.Seek(chunk.Length, SeekOrigin.Current);

            ParseRecursively(fs);
        }

        private void PostCheck(Stream fs)
        {
            Sanity.Requires(!string.IsNullOrEmpty(FormatChunk.Name), "Invalid wave file, missing format chunk.");
            Sanity.Requires(!string.IsNullOrEmpty(DataChunk.Name), "Invalid wave file, missing data chunk.");
            PostCheckFormatChunk(fs);
            if (IsDeep)
                PostCheckDataChunk(fs);
        }

        private void PostCheckFormatChunk(Stream fs)
        {
            fs.Seek(FormatChunk.Offset, SeekOrigin.Begin);
            WaveType = fs.ReadShortFromFileStream();
            NumChannels = fs.ReadShortFromFileStream();
            SampleRate = fs.ReadIntFromFileStream();
            ByteRate = fs.ReadIntFromFileStream();
            BlockAlign = fs.ReadShortFromFileStream();
            BitsPerSample = fs.ReadShortFromFileStream();

            Sanity.Requires(ByteRate == SampleRate * BlockAlign, $"Invalid audio: ByteRate: {ByteRate}, SampleRate: {SampleRate}, BlockAlign: {BlockAlign}.");
            Sanity.Requires(BitsPerSample * NumChannels == 8 * BlockAlign, $"Invalid audio: BitsPerSample: {BitsPerSample}, NumChannels: {NumChannels}, BlockAlign: {BlockAlign}");

            AudioLength = (double)DataChunk.Length / ByteRate;
        }


        private void PostCheckDataChunk(Stream fs)
        {
            fs.Seek(DataChunk.Offset + 8, SeekOrigin.Begin);
            byte[] buffer = new byte[BitsPerSample / 8 * BUFFER_SIZE];
            Func<byte[], int, long> readSquareSum = null;
            switch (BitsPerSample)
            {
                case 8:
                    readSquareSum = ReadBytesSquareSum;
                    break;
                case 16:
                    readSquareSum = ReadShortsSquareSum;
                    break;
                default:
                    break;
            }
            Sanity.Requires(readSquareSum != null, $"Unsupported bits per sample: {BitsPerSample}");
            long l = 0;
            int totalSamples = 0;
            while (fs.Position < fs.Length)
            {
                int n = fs.Read(buffer, 0, buffer.Length);
                l += readSquareSum(buffer, n);
                totalSamples += n;
            }
            _RMS = Math.Sqrt((double)l / totalSamples);
        }

        private long ReadBytesSquareSum(byte[] bytes, int n)
        {
            long l = 0;
            for(int i = 0; i < n; i++)
            {
                l += bytes[i] * bytes[i];
            }
            return l;
        }

        private long ReadShortsSquareSum(byte[] bytes, int n)
        {
            long l = 0;
            for(int i = 0; i < n; i += 2)
            {
                short s = BitConverter.ToInt16(bytes, i);
                l += s * s;
            }
            return l;
        }
    }

    public struct WaveChunk
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public int Offset { get; set; }
    }
}
