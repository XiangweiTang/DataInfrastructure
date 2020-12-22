using System;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DICommon
{
    public static class AzureUtils
    {
        public static readonly string SAS_TOKEN = "";
        static AzureUtils()
        {
            SAS_TOKEN = File.ReadAllText(@"C:\Files\Azure\SASToken.txt").Trim();
        }
        public const string BlobContainerName = "/chdatacollections/";
        public static async Task<List<string>> ListDirectoriesAsync(string uriString)
        {
            Uri tmpUri = new Uri(uriString);
            string prefix = tmpUri.LocalPath.Substring(BlobContainerName.Length);

            Uri uri = new Uri($"https://{tmpUri.Host}{BlobContainerName}");
            BlobContainerClient client = new BlobContainerClient(uri);
            
            
            var pages = client.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/").AsPages();
            List<string> list = new List<string>();
            await foreach(Page<BlobHierarchyItem> page in pages)
            {
                list.AddRange(page.Values.Where(x => x.IsPrefix).Select(x => $"{tmpUri.OriginalString}{x.Prefix}"));
            }
            return list;
        }

        public static async Task<List<string>> ListBlobsAsync(string uriString)
        {
            Uri tmpUri = new Uri(uriString);
            string prefix = tmpUri.LocalPath.Substring(BlobContainerName.Length);

            Uri uri = new Uri($"https://{tmpUri.Host}{BlobContainerName}");
            BlobContainerClient client = new BlobContainerClient(uri);
            var pages = client.GetBlobsAsync(prefix: prefix).AsPages();

            List<string> list = new List<string>();
            await foreach(Page<BlobItem> page in pages)
            {
                list.AddRange(page.Values.Select(x => $"{uri.OriginalString}{x.Name}"));
            }
            return list;
        }

        public static Stream ReadBlobToStream(string uriString)
        {
            Uri uri = new Uri(uriString);
            BlobClient client = new BlobClient(uri);
            return client.OpenRead(new BlobOpenReadOptions(false));
        }

        public static void Download(string uriString, string localPath)
        {
            Uri uri = new Uri(uriString);
            BlobClient client = new BlobClient(uri);
            client.DownloadTo(localPath);
        }

        public static void Upload(string localPath, string uriString)
        {
            Uri uri = new Uri(uriString+SAS_TOKEN);
            BlobClient client = new BlobClient(uri);
            client.Upload(localPath);
        }
    }
}
