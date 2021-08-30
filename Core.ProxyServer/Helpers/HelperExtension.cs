using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Core.ProxyServer.Helpers
{
    public static class HelperExtension
    {
        //public static async Task<Socket> PipeSenderAsync(this Socket source, Socket target)
        //{
        //    try
        //    {
        //        while (source.Connected && target.Connected)
        //        {
        //            var data = await source.ReceiveAsync()


        //        }
        //    }
        //    catch { }
        //    return target;
        //}


        //Start Pipe : https://docs.microsoft.com/en-us/dotnet/standard/io/pipelines
        public static async Task ProcessLinesAsync(this Socket socket, Func<ReadOnlySequence<byte>, Task> callback, CancellationToken cancellationToken = default)
        {
            var pipe = new Pipe();
            Task writing = socket.FillPipeAsync(pipe.Writer, cancellationToken);
            Task reading = socket.ReadPipeAsync(pipe.Reader, callback, cancellationToken);
            await Task.WhenAll(reading, writing);
        }
        private static async Task FillPipeAsync(this Socket socket, PipeWriter writer, CancellationToken cancellationToken = default)
        {
            const int minimumBufferSize = 512;

            while (true)
            {
                // Allocate at least 512 bytes from the PipeWriter.
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                try
                {
                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    // Tell the PipeWriter how much was read from the Socket.
                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    break;
                }

                // Make the data available to the PipeReader.
                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // By completing PipeWriter, tell the PipeReader that there's no more data coming.
            await writer.CompleteAsync();
        }
        private static async Task ReadPipeAsync(this Socket socket, PipeReader reader, Func<ReadOnlySequence<byte>, Task> callback, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                {
                    // Process the line.
                    await callback(line);
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming.
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();
        }
        private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            // Look for a EOL in the buffer.
            SequencePosition? position = buffer.PositionOf((byte)'\n');

            if (position == null)
            {
                line = default;
                return false;
            }

            // Skip the line + the \n.
            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            return true;
        }
        //End Pipe 
    }
}
