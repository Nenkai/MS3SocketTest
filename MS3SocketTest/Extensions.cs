using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Buffers.Binary;

namespace MS3SocketTest;

public static class Extensions
{
    public static async Task SendEvoMessage(this Socket socket, string str)
    {
        // 4 bytes unk
        // 4 bytes buffer length of next packet
        int bufLen = Encoding.UTF8.GetByteCount(str);
        byte[] buf = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(4), (uint)bufLen + 1);
        await socket.SendAsync(buf);

        // Second packet
        // Buffer
        byte[] buffer = new byte[bufLen + 1];
        Encoding.UTF8.GetBytes(str, buffer);
        await socket.SendAsync(buffer);
    }
}
