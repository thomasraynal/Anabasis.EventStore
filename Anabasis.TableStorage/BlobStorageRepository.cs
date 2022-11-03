using Azure.Storage.Blobs;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Anabasis.TableStorage
{
    public class BlobStorageRepository : IBlobStorageRepository
    {
        private readonly ConcurrentDictionary<string, BlobContainerClient> _blobContainerClientCache;
        private readonly string _tableStorageConnectionString;
        private readonly HttpClient _httpClient;

        public BlobStorageRepository(string connectionString, HttpClient httpClient)
        {
            _blobContainerClientCache = new ConcurrentDictionary<string, BlobContainerClient>();
            _tableStorageConnectionString = connectionString;
            _httpClient = httpClient;
        }

        private Uri GetBlobUri(string blobContainerPath, string blobContainerName)
        {
            var blobContainerClient = GetBlobContainerClient(blobContainerName);

            return new Uri(Path.Combine(blobContainerClient.Uri.AbsolutePath, blobContainerPath));
        }

        private BlobContainerClient GetBlobContainerClient(string blobContainerName)
        {
            return _blobContainerClientCache.GetOrAdd(blobContainerName, (key) =>
            {
                return new BlobContainerClient(_tableStorageConnectionString, blobContainerName);
            });
        }

        public async Task DownloadFile(string localPath, string blobContainerPath, string blobContainerName)
        {

            var blobContainerClient = GetBlobContainerClient(blobContainerName);
            var uri = GetBlobUri(blobContainerClient.Uri.AbsolutePath, blobContainerPath);

            var httpResponseMessage = await _httpClient.GetAsync(uri);

            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }

            using var fileStream = new FileStream(localPath, FileMode.OpenOrCreate);

            await httpResponseMessage.Content.CopyToAsync(fileStream);

        }

        public async Task<string> ReadFile(string blobContainerPath, string blobContainerName)
        {
            var blobContainerClient = GetBlobContainerClient(blobContainerName);
            var uri = GetBlobUri(blobContainerClient.Uri.AbsolutePath, blobContainerPath);

            return await _httpClient.GetStringAsync(uri);

        }

        public async Task<Uri> UploadFile(string localPath, string blobContainerPath, string blobContainerName)
        {
            using var fileStream = File.OpenRead(localPath);

            return await UploadFile(fileStream, blobContainerPath, blobContainerName);
        }

        public async Task<Uri> UploadFile(Stream stream, string blobContainerPath, string blobContainerName)
        {
            var blobContainerClient = GetBlobContainerClient(blobContainerName);

            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(blobContainerPath);

            using var memoryStream = new MemoryStream();

            stream.CopyTo(memoryStream);

            memoryStream.Position = 0;

            var response = await blobClient.UploadAsync(memoryStream, overwrite: true);

            var uri = GetBlobUri(blobContainerClient.Uri.AbsolutePath, blobContainerPath);

            return uri;
        }
    }
}
