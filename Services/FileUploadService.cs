using FileUploader.WPF.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FileUploader.WPF.Services
{
    public class FileUploadService
        {
            private static log4net.ILog logger = log4net.LogManager.GetLogger("MainLog");
            private const int CHUNK_SIZE = 5 * 1024 * 1024; // 5MB
            private readonly HttpService _httpService;
            private readonly SemaphoreSlim _semaphore;

            public FileUploadService(string baseUrl)
            {
                _httpService = new HttpService(baseUrl);
                _semaphore = new SemaphoreSlim(30); // 最多30个并发上传
            }

            public async Task<string> CalculateOptimizedHashAsync(string filePath)
            {
                // 对于图片文件，只读取前1MB数据计算哈希
                const int SAMPLE_SIZE = 1 * 1024 * 1024;
                
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                
                // 添加文件大小
                var fileInfo = new FileInfo(filePath);
                string sizeInfo = fileInfo.Length.ToString();
                
                byte[] sizeBytes = Encoding.UTF8.GetBytes(sizeInfo);
                md5.TransformBlock(sizeBytes, 0, sizeBytes.Length, null, 0);
                
                // 读取文件头部
                byte[] buffer = new byte[Math.Min(SAMPLE_SIZE, (int)fileInfo.Length)];
                await stream.ReadAsync(buffer);
                md5.TransformFinalBlock(buffer, 0, buffer.Length);
                
                return BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
            }

            public async Task<List<ChunkInfo>> SplitFileAsync(string filePath)
            {
                var chunks = new List<ChunkInfo>();
                using var stream = File.OpenRead(filePath);

                var buffer = new byte[CHUNK_SIZE];
                int index = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    var chunk = new byte[bytesRead];
                    Array.Copy(buffer, chunk, bytesRead);

                    using var md5 = MD5.Create();
                    var hash = md5.ComputeHash(chunk);

                    chunks.Add(new ChunkInfo
                    {
                        Data = chunk,
                        Index = index,
                        Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()
                    });

                    index++;
                }

                return chunks;
            }

            public async Task<bool> UploadFileAsync(string filePath, IProgress<UploadFileInfo> progress)
            {
                var fileInfo = new FileInfo(filePath);
                var uploadInfo = new UploadFileInfo
                {
                    originFileName = fileInfo.Name,
                    size = fileInfo.Length,
                    chunkCount = (int)Math.Ceiling(fileInfo.Length / (double)CHUNK_SIZE)
                };

                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                
                uploadInfo.md5 = await CalculateOptimizedHashAsync(filePath);
                progress.Report(uploadInfo);

                stopwatch.Stop();
                Trace.WriteLine($"哈希值计算时间：{stopwatch.ElapsedMilliseconds}ms");
                logger.Info($"哈希值计算时间：{stopwatch.ElapsedMilliseconds}ms");

                // 检查文件是否已存在
                var checkResult = await _httpService.GetAsync<ApiResponse<UploadFileInfo>>($"/files/multipart/check/{uploadInfo.md5}");
                if (checkResult.Code == 2001) // 文件已存在
                {
                    uploadInfo.Progress = 100;
                    uploadInfo.Status = "Completed";
                    progress.Report(uploadInfo);
                    return true;
                }

            var stopwatch1 = new System.Diagnostics.Stopwatch();
            stopwatch1.Start();

                // 初始化分片上传
                var initResult = await _httpService.PostAsync<ApiResponse<InitUploadResponse>>("/files/multipart/init", new
                {
                    uploadInfo.uploadId,
                    uploadInfo.originFileName,
                    uploadInfo.size,
                    uploadInfo.md5,
                    chunkSize = CHUNK_SIZE,
                    uploadInfo.chunkCount
                });

            //Console.WriteLine("上传成功后的回复：" + initResult);
                  
                //if (initResult.Code != 200)
                //    return false;

                uploadInfo.uploadId = initResult.Data.uploadId;
                uploadInfo.Status = "Uploading";
                progress.Report(uploadInfo);

                // 分片上传
                var chunks = await SplitFileAsync(filePath);
                var tasks = new List<Task>();

                foreach (var chunk in chunks)
                {
                    if (uploadInfo.UploadedParts.Contains(chunk.Index))
                        continue;

                    var task = UploadChunkAsync(chunk, initResult.Data.urls[chunk.Index], uploadInfo, progress);
                    tasks.Add(task);

                    if (tasks.Count >= 3)
                    {
                        await Task.WhenAny(tasks.ToArray());
                        tasks.RemoveAll(t => t.IsCompleted);
                    }
                }

                await Task.WhenAll(tasks);

                // 合并文件
                var mergeResult = await _httpService.PostAsync<ApiResponse<object>>($"/files/multipart/merge/{uploadInfo.md5}", null);

                stopwatch1.Stop();
                Trace.WriteLine($"上传时间：{stopwatch1.ElapsedMilliseconds}ms");
                logger.Info($"上传时间：{stopwatch1.ElapsedMilliseconds}ms");

                uploadInfo.Progress = 100;
                uploadInfo.Status = mergeResult.Code == 200 ? "Completed" : "Failed";
                progress.Report(uploadInfo);

                return mergeResult.Code == 200;
            }

            private async Task UploadChunkAsync(ChunkInfo chunk, string url, UploadFileInfo uploadInfo, IProgress<UploadFileInfo> progress)
            {
                await _semaphore.WaitAsync();
                try
                {
                    var success = await _httpService.PutFileChunkAsync(url, chunk.Data, "application/octet-stream");
                    if (success)
                    {
                        uploadInfo.UploadedSize += chunk.Data.Length;
                        uploadInfo.Progress = (int)((double)uploadInfo.UploadedSize / uploadInfo.size * 100);
                        uploadInfo.UploadedParts.Add(chunk.Index);
                        progress.Report(uploadInfo);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
}
