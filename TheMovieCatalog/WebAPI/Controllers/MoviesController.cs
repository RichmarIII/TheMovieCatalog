
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheMovieCatalog.DataModels;
using TheMovieCatalog.Shared;
using TheMovieCatalog.Shared.ExtensionMethods;

namespace TheMovieCatalog.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    public class MoviesController : Controller
    {
        public ConcurrentBag<StreamWriter> clients { get; private set; } = new ConcurrentBag<StreamWriter>();

        public async Task WriteContentToStream(Stream outputStream, ActionContext context)
        {
            byte[] buffer = new byte[1024 * 4];
            if (bufferedStream != null)
            {
                int len;
                while ((len = await bufferedStream.ReadAsync(buffer)) > 0)
                {
                    await outputStream.WriteAsync(buffer.AsMemory(0, len));
                }
            }
        }

        private BufferedStream? bufferedStream;

        [HttpGet]
        [Route("{MovieID}")]
        [Produces("application/octet-stream")]
        async public Task<FileCallbackResult?> Play(string MovieID = "")
        {
            var movieID = Guid.Parse(MovieID);
            var movie = await App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (MainWindow.Instance.Libraries.IsNullOrEmpty())
                    return null;
                else
                {
                    var movieLibraries = MainWindow.Instance.Libraries.Where((l) => l.MediaLibraryType == MediaLibraryType.Movies);
                    if (movieLibraries.IsNullOrEmpty())
                        return null;
                    else
                    {
                        foreach (var movieLibrary in movieLibraries)
                        {
                            var movie = movieLibrary.MediaMetaDatas.Find((m) => ((MovieData)m).ID == movieID) as MovieData;
                            if (movie != null)
                                return movie;
                        }
                    }
                    return null;
                }
            });

            if (movie == null)
                return null;

            Process? process = null;

            ProcessStartInfo SI = new ProcessStartInfo("ffmpeg", String.Format("-y -re -ss 01:32:00 -i \"{0}\" -movflags +faststart -crf 27 -tune zerolatency -bufsize 4M -maxrate 4M -c:v libx264 -c:a aac -f mpegts -", movie.LocalFilePath));
            SI.RedirectStandardOutput = true;
            SI.UseShellExecute = false;
            SI.CreateNoWindow = false;

            process = Process.Start(SI);

            if (process != null)
                bufferedStream = new BufferedStream(process.StandardOutput.BaseStream, 1024 * 1000);


            return new FileCallbackResult("application/octet-stream", async (stream, _) =>
            {
                Stopwatch stopwatch = new Stopwatch();
                if (bufferedStream != null)
                {
                    stopwatch.Start();
                    var buff = new byte[1024 * 1000];
                    int count;
                    while (true)
                    {
                        count = await bufferedStream.ReadAsync(buff);
                        if (count > 0)
                        {
                            await stream.WriteAsync(buff.AsMemory(0, count));
                            stopwatch.Restart();
                        }
                        else
                        {
                            if (stopwatch.ElapsedMilliseconds > 10000)
                            {
                                process.Kill(true);
                            }
                        }
                    }
                }
            });
        }
    }
}
