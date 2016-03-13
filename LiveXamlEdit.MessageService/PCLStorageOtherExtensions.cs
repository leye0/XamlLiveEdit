using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCLStorage
{
    /// <summary>
    /// Provides extension methods for the <see cref="IFile"/> class
    /// </summary>
    public static class FileExtensions
    {
		public static async Task<byte[]> FileReadAllBytes (this IFile file)
		{
			var data = new byte[0];

			using (var fileStream = await file.OpenAsync(FileAccess.ReadAndWrite))
			using (var memoryStream = new MemoryStream())
	        {
				fileStream.CopyTo(memoryStream);
				data = memoryStream.ToArray();
	        }

	        return data;
		}

		public static void WriteAllBytes(this IFile file, byte[] contents)
        {
            using (var stream = file.OpenAsync(FileAccess.ReadAndWrite).Result)
            {
                stream.SetLength(0);
                using (var sw = new BinaryWriter(stream))
                {
                    sw.Write(contents);
                }
            }
        }
    }
}