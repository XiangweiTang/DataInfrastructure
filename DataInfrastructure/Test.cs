using System;
using System.Collections.Generic;
using System.Text;
using DICommon;
using System.IO;

namespace DataInfrastructure
{
    class Test
    {
        public Test()
        {
            string path = @"https://marksystemapistorage.blob.core.windows.net/chdatacollections/300hrsRecordingContent/Basel/Basel%2001-1.12.2020/";
            var r = AzureUtils.ListBlobsAsync(path).Result;
            foreach(string s in r)
            {
                Wave w = new Wave();
                using(Stream st = AzureUtils.ReadBlobToStream(s))
                {
                    w.ShallowParse(st);
                    Console.WriteLine(s);
                    Console.WriteLine(w.SampleRate);
                    Console.WriteLine(w.NumChannels);
                }
            }
        }
    }
}
