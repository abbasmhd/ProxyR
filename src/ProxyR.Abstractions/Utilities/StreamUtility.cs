using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ProxyR.Abstractions.Utilities
{
    public class StreamUtility
    {
        public static async Task<(Stream InMemoryStream, string Text)> ReadAsStringAsync(Stream stream)
        {
            // Do we have a body?
            if (stream == null || stream == Stream.Null)
            {
                return (stream, null);
            }

            // Try and seek to the beginning.
            // This is typically done on the response after a handler.
            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            string text;

            // Create a memory-stream to buffer the request.
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);

                try
                {
                    // Let's try and get this into a String.
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream: memoryStream, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
                    {
                        text = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }

                    // Translate an empty body to a NULL.
                    if (String.IsNullOrEmpty(text))
                    {
                        text = null;
                    }
                }
                finally
                {
                    // Does the body support seeking?
                    if (stream.CanSeek)
                    {
                        // Seek back for the next handler?
                        stream.Seek(0, SeekOrigin.Begin);
                    }
                    else
                    {
                        // Set the body to the entirely read content for the next handler to use.
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        stream = memoryStream;
                    }
                }
            }

            return (stream, text);
        }
    }

}
