﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace DICommon
{
    public static class IO
    {
        public static string ReadStringFromFileStream(this Stream fs, Encoding encoding, int count)
        {
            byte[] buffer = new byte[count];
            fs.Read(buffer, 0, count);
            return encoding.GetString(buffer);
        }

        public static int ReadIntFromFileStream(this Stream fs)
        {
            byte[] buffer = new byte[4];
            fs.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static short ReadShortFromFileStream(this Stream fs)
        {
            byte[] buffer = new byte[2];
            fs.Read(buffer, 0, 2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static IEnumerable<string> ReadEmbed(string name, string asmbName="DICommon")
        {
            Assembly asmb = Assembly.Load(asmbName);
            using(StreamReader sr=new StreamReader(asmb.GetManifestResourceStream(name)))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    yield return line;
            }
        }

        public static string ReadEmbedAll(string name, string asmbName = "DICommon")
        {
            Assembly asmb = Assembly.Load(asmbName);
            using (StreamReader sr = new StreamReader(asmb.GetManifestResourceStream(name)))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
