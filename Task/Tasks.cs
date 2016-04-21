using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading;

namespace Task
{
    public static class Tasks
    {
        /// <summary>
        /// Returns the content of required uri's.
        /// Method has to use the synchronous way and can be used to compare the
        ///  performace of sync/async approaches. 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContent(this IEnumerable<Uri> uris)
        {
            using (WebClient webClient = new WebClient())
            {
                foreach (var uri in uris)
                {
                    yield return webClient.DownloadString(uri);
                }
            }
        }

        /// <summary>
        /// Returns the content of required uris.
        /// Method has to use the asynchronous way and can be used to compare the performace 
        /// of sync \ async approaches. 
        /// maxConcurrentStreams parameter should control the maximum of concurrent streams 
        /// that are running at the same time (throttling). 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <param name="maxConcurrentStreams">Max count of concurrent request streams</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContentAsync(this IEnumerable<Uri> uris, int maxConcurrentStreams)
        {
            SemaphoreSlim throttler = new SemaphoreSlim(maxConcurrentStreams, maxConcurrentStreams);
            Task<string>[] allTasks = new Task<string>[uris.Count()];
            List<string> contents = new List<string>();

            using (HttpClient client = new HttpClient())
            {
                int i = 0;
                foreach (var uri in uris)
                {
                    throttler.Wait();
                    allTasks[i++] = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            return await client.GetStringAsync(uri);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    });
                }

                System.Threading.Tasks.Task.WaitAll(allTasks);
                foreach (var task in allTasks)
                {
                    contents.Add(task.Result);
                }

                return contents;
            }
        }

        /// <summary>
        /// Calculates MD5 hash of required resource.
        /// 
        /// Method has to run asynchronous. 
        /// Resource can be any of type: http page, ftp file or local file.
        /// </summary>
        /// <param name="resource">Uri of resource</param>
        /// <returns>MD5 hash</returns>
        async public static Task<string> GetMD5Async(this Uri resource)
        {
            using (WebClient webClient = new WebClient())
            {
                Stream resourceArray = await webClient.OpenReadTaskAsync(resource);

                MD5 hash = MD5.Create();
                byte[] hashArray = hash.ComputeHash(resourceArray);

                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < hashArray.Length; i++)
                {
                    sBuilder.Append(hashArray[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }

    }
}
