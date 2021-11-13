using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Storage
{
    public interface IFile
    {
        string FilePath { get; }
    }

    public interface IFileStorage
    {
        Task WriteTextAsync(object requestFile, string fileContent, string mimeType);
    }
}
