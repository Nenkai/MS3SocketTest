using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MS3SocketTest;

internal class Program
{
    public const int NetworkFileSystem_Port = 60030;
    public const int RemoteControlServer_Port = 60031;

    private static Socket _remoteClient;

    public static List<string> _commandBuffer = new List<string>();

    static async Task Main(string[] args)
    {
        var fileSysTask = DoNetworkFileSystemServer();
        var remoteControlServer = DoRemoteControlServer();

        Console.WriteLine("Waiting...");

        while (true)
        {
            if (_remoteClient is null || !_remoteClient.Connected)
                continue;

            Console.Write(">");
            string str = Console.ReadLine();

            if (!string.IsNullOrEmpty(str))
            {
                if (str.StartsWith("Load"))
                {
                    string[] spl = str.Split(" ");
                    if (spl.Length < 2)
                    {
                        Console.WriteLine("Usage: Load <rpk files>");
                        continue;
                    }

                    if (!File.Exists(Path.Combine("<full path to app_home\\published>", spl[1])))
                    {
                        Console.WriteLine("ERROR: File does not exist, aborting load otherwise game crashes");
                        continue;
                    }
                }

                await _remoteClient.SendEvoMessage(str);
            }
        }
    }

    // Controls:
    // L3: Stop Update (?)

    // Commands:
    // "Load <rpk paths separated by spaces>" - You should load a env rpk first
    //
    // "LoadCharAnimPack <rpk paths separated by spaces>"
    // example:
    //    Load characters/f_festival_07_LV01.rpk
    //    LoadCharAnimPack animation_packs/m_npc.rpk
    //
    // "camerasync#%f,%f,%f,%f,%f,%f,%f,%f,%f,%f,%f,%f" - Sets camera to world matrix (4x3)
    //      example: "camerasync#1.0,0.0,0.0,0.0,1.0,0.0,0.0,0.0,1.0,0.0,0.0,1.0"
    //
    // "evotweak#set#<pMenuItem>#<pMenuValue>"
    //      pMenuItem is tokens separated by ;
    // "evotweak#getdata"

    static async Task DoNetworkFileSystemServer()
    {
        IPEndPoint ipEndPoint = new(IPAddress.Any, NetworkFileSystem_Port);
        using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(ipEndPoint);
        listener.Listen(100);

        var handler = await listener.AcceptAsync();
        Console.WriteLine("Got NetworkFileSystem client connection");

        while (handler.Connected)
        {
            // Receive message.
            var buffer = new byte[1_024];
            var received = await handler.ReceiveAsync(buffer, SocketFlags.None);

            // This is evo::streams::SocketOStream::SocketOStream
            if (received == 4 && BinaryPrimitives.ReadUInt32BigEndian(buffer) == 0x12345678)
            {
                await handler.SendAsync(BitConverter.GetBytes(0x12345678).Reverse().ToArray());
            }
            else
            {
                var msg = Encoding.UTF8.GetString(buffer, 0, received);
                if (msg.StartsWith("Evo NetworkFileSystem Version 1. Hello."))
                {
                    await handler.SendAsync(Encoding.UTF8.GetBytes("Hello. Connection accepted.\0"));
                }
                else
                {
                    ;
                }
            }
        }
    }

    // Client created at evo::remotecontrol::ReceiverInit
    // evo::remotecontrol
    static async Task DoRemoteControlServer()
    {
        IPEndPoint ipEndPoint = new(IPAddress.Any, RemoteControlServer_Port);
        using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(ipEndPoint);
        listener.Listen(100);

        while (true)
        {
            _remoteClient = await listener.AcceptAsync();

            Console.WriteLine("Got RemoteControl client connection");
            await HandleClient(listener, _remoteClient);
        }
    }

    private static async Task HandleClient(Socket listener, Socket handler)
    {
        CancellationToken ct = new CancellationToken();

        try
        {
            while (handler.Connected)
            {
                // Receive message.
                var buffer = new byte[1_024];
                var received = await handler.ReceiveAsync(buffer, SocketFlags.None, cancellationToken: ct);

                // This is evo::streams::SocketOStream::SocketOStream
                if (received == 4)
                {
                    uint value = BinaryPrimitives.ReadUInt32BigEndian(buffer);
                    if (value == 0x12345678) // Client sends endian - 0x12345678 = big
                    {
                        // We'll use big aswell
                        await handler.SendAsync(BitConverter.GetBytes(0x12345678).Reverse().ToArray(), cancellationToken: ct);
                    }
                    else
                    {
                        byte[] strBuf = new byte[value];
                        int read = await handler.ReceiveAsync(strBuf, cancellationToken: ct);
                    }
                }
                else
                {
                    var msg = Encoding.UTF8.GetString(buffer, 0, received);
                    if (msg.StartsWith("Evo RemoteControl Server. Hello."))
                    {
                        await handler.SendAsync(Encoding.UTF8.GetBytes("Hello. Connection accepted.\0"), cancellationToken: ct);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            handler.Dispose();
        }
        finally
        {
            Console.WriteLine($"RemoteControl Disconnected");
        }
    }
}
