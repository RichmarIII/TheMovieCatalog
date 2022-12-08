
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheMovieCatalog.Shared;
using TheMovieCatalog.Shared.ExtensionMethods;
using TheMovieCatalog.WebAPI.DataModels;
using TheMovieCatalog.WebAPI.DataModels.Helpers;

namespace TheMovieCatalog.WebAPI.Controllers
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// execute a callback to write the file content out as a stream.
    /// </summary>
    public class FileCallbackResult : FileResult
    {
        private Func<Stream, ActionContext, Task>? _callback;

        /// <summary>
        /// Creates a new <see cref="FileCallbackResult"/> instance.
        /// </summary>
        /// <param name="contentType">The Content-Type header of the response.</param>
        /// <param name="callback">The stream with the file.</param>
        public FileCallbackResult(string contentType, Func<Stream, ActionContext, Task> callback)
            : this(MediaTypeHeaderValue.Parse(contentType), callback)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FileCallbackResult"/> instance.
        /// </summary>
        /// <param name="contentType">The Content-Type header of the response.</param>
        /// <param name="callback">The stream with the file.</param>
        public FileCallbackResult(MediaTypeHeaderValue contentType, Func<Stream, ActionContext, Task> callback)
            : base(contentType?.ToString())
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            Callback = callback;
        }

        /// <summary>
        /// Gets or sets the callback responsible for writing the file content to the output stream.
        /// </summary>
        public Func<Stream, ActionContext, Task>? Callback
        {
            get
            {
                return _callback;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _callback = value;
            }
        }

        /// <inheritdoc />
        public override Task? ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            if (executor != null)
                return executor.ExecuteAsync(context, this);
            return null;
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            public Task? ExecuteAsync(ActionContext context, FileCallbackResult result)
            {
                SetHeadersAndLog(context, result, null, true);
                if (result.Callback != null)
                    return result.Callback(context.HttpContext.Response.Body, context);
                return null;
            }
        }
    }

    class NonClosingNonSeekableStream : Stream
    {
        public NonClosingNonSeekableStream(Stream tail)
        {
            this.tail = tail ?? throw new ArgumentNullException(nameof(tail));
        }

        private readonly Stream tail;
        public override bool CanRead
        {
            get { return tail.CanRead; }
        }
        public override bool CanWrite
        {
            get { return tail.CanWrite; }
        }
        public override bool CanSeek
        {
            get { return tail.CanSeek; }
        }
        public override bool CanTimeout
        {
            get { return tail.CanTimeout; }
        }
        public override long Position
        {
            get { return tail.Position; }
            set { tail.Position = value; }
        }
        public override void Flush()
        {
            tail.Flush();
        }
        public override void SetLength(long value)
        {
            tail.SetLength(value);
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return tail.Seek(offset, origin);
        }
        public override long Length
        {
            get { return tail.Length; }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return tail.Read(buffer, offset, count);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            tail.Write(buffer, offset, count);
        }
        public override int ReadByte()
        {
            return tail.ReadByte();
        }
        public override void WriteByte(byte value)
        {
            tail.WriteByte(value);
        }
        public void Reset()
        {

        }
    }

    [Route("api/[controller]/[action]")]
    public class LibrariesController : Controller
    {
        [HttpGet]
        async public Task<List<MoviesLibraryDataWebAPI>?> Movies()
        {
            return await App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (MainWindow.Instance.Libraries.IsNullOrEmpty())
                    return null;
                else
                {
                    var Libraries = MainWindow.Instance.Libraries.Where((l) => l.MediaLibraryType == MediaLibraryType.Movies).Select((l) => WebAPIDataFactory.CreateMoviesLibraryDataWebAPI(l)).ToList();
                    if (Libraries.IsNullOrEmpty())
                        return null;
                    else
                        return Libraries;
                }
            });
        }
    }
}
