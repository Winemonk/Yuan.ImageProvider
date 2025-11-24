using System.Security.Cryptography;
using System.Text;

namespace Yuan.ImageProvider.Utils
{
    public class CryptographyUtil
    {
        /// <summary>
        /// 计算文件的MD5值
        /// </summary>
        public static async Task<string> CalculateFileMD5Async(string filePath, IProgress<long>? progress = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await CalculateStreamMD5Async(stream, progress);
        }

        /// <summary>
        /// 计算文件的MD5值
        /// </summary>
        public static async Task<string> CalculateStreamMD5Async(Stream stream, IProgress<long>? progress = null)
        {
            using var md5 = MD5.Create();
            byte[] buffer = new byte[1024 * 80]; // 80KB缓冲区
            long totalBytesRead = 0;
            long fileLength = stream.Length;

            md5.Initialize();

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
            stream.Position = 0;
            md5.TransformFinalBlock(buffer, 0, 0);
            if (md5.Hash == null)
            {
                return string.Empty;
            }
            return BytesToHex(md5.Hash);
        }

        /// <summary>
        /// 将字节数组转换为十六进制字符串
        /// </summary>
        private static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}