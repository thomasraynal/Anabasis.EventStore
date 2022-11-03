using System;
using System.IO;
using System.Threading.Tasks;

namespace Anabasis.TableStorage
{
    public interface IBlobStorageRepository
    {
        Task DownloadFile(string localPath, string blobContainerPath, string blobContainerName);
        Task<string> ReadFile(string blobContainerPath, string blobContainerName);
        Task<Uri> UploadFile(Stream stream, string blobContainerPath, string blobContainerName);
        Task<Uri> UploadFile(string localPath, string blobContainerPath, string blobContainerName);
    }
}