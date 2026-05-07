using System;
using System.Text;

namespace ENet6.Example
{
    /// <summary>
    /// Minimal example showing how to bind a server to ENET_HOST_ANY (Address.BuildAny)
    /// so it accepts connections on every interface, then connect a local client to it
    /// and exchange a single message.
    ///
    /// Usage:
    ///   ENet6.Example                     run server + client in one process (default)
    ///   ENet6.Example server [port]       run only the server
    ///   ENet6.Example client host port    run only the client
    /// </summary>
    internal static class Program
    {
        private const ushort DefaultPort = 7777;

        private static int Main(string[] args)
        {
            if (!Library.Initialize())
            {
                Console.Error.WriteLine("Library.Initialize failed");
                return 1;
            }

            try
            {
                if (args.Length == 0)
                {
                    InProcessDemo();
                }
                else if (args[0] == "server")
                {
                    ushort port = args.Length >= 2 ? ushort.Parse(args[1]) : DefaultPort;
                    RunServer(port, runForever: true);
                }
                else if (args[0] == "client" && args.Length >= 3)
                {
                    RunClient(args[1], ushort.Parse(args[2]));
                }
                else
                {
                    Console.Error.WriteLine("Unknown arguments. See file header for usage.");
                    return 2;
                }
            }
            finally
            {
                Library.Deinitialize();
            }

            return 0;
        }

        private static void InProcessDemo()
        {
            Console.WriteLine("Running self-contained server + client demo on port " + DefaultPort);

            // Bind the server to ENET_HOST_ANY on the IPv6 family. Because this fork's IPv6
            // sockets accept IPv4-mapped connections, a single Any/IPv6 host can serve both
            // IPv4 and IPv6 clients on the same port.
            using (Host server = new Host())
            using (Host client = new Host())
            {
                Address bind = Address.BuildAny(AddressType.IPv6);
                bind.Port = DefaultPort;

                server.Create(AddressType.Any, bind, peerLimit: 32, channelLimit: 2);
                Console.WriteLine("server: bound to [::]:" + DefaultPort + "  IsAny=" + bind.IsAny());

                client.Create(AddressType.IPv6);

                Address dst = Address.BuildLoopback(AddressType.IPv6);
                dst.Port = DefaultPort;

                Peer remote = client.Connect(dst, channelLimit: 2);
                Console.WriteLine("client: connecting to " + dst.GetIP());

                bool clientConnected = false;
                bool gotEcho = false;
                long started = Environment.TickCount;

                while (Environment.TickCount - started < 5000 && !gotEcho)
                {
                    PumpServer(server);
                    PumpClient(client, remote, ref clientConnected, ref gotEcho);
                }

                if (!gotEcho)
                    Console.Error.WriteLine("client: timed out waiting for echo");

                remote.Disconnect(0);

                long disconnectStarted = Environment.TickCount;
                while (Environment.TickCount - disconnectStarted < 1000)
                {
                    PumpServer(server);
                    Event ev;
                    client.Service(50, out ev);
                }
            }

            Console.WriteLine("done");
        }

        private static void RunServer(ushort port, bool runForever)
        {
            using (Host server = new Host())
            {
                Address bind = Address.BuildAny(AddressType.IPv6);
                bind.Port = port;
                server.Create(AddressType.IPv6, bind, peerLimit: 32, channelLimit: 2);
                Console.WriteLine("server: listening on [::]:" + port);

                while (runForever)
                {
                    PumpServer(server);
                }
            }
        }

        private static void RunClient(string host, ushort port)
        {
            using (Host client = new Host())
            {
                client.Create(AddressType.IPv6);

                Address dst = new Address();
                dst.Type = AddressType.IPv6;
                if (!dst.SetHost(AddressType.IPv6, host))
                    throw new InvalidOperationException("Could not resolve " + host);
                dst.Port = port;

                Peer remote = client.Connect(dst, 2);
                bool connected = false;
                bool gotEcho = false;
                long started = Environment.TickCount;

                while (Environment.TickCount - started < 5000 && !gotEcho)
                {
                    PumpClient(client, remote, ref connected, ref gotEcho);
                }
            }
        }

        /// <summary>
        /// Server side: dispatch one event slice. Echoes any received packet back to its sender.
        /// </summary>
        private static void PumpServer(Host server)
        {
            Event ev;
            int rc = server.Service(15, out ev);
            if (rc <= 0)
                return;

            switch (ev.Type)
            {
                case EventType.Connect:
                    Console.WriteLine("server: connect from " + ev.Peer.IP + " (id=" + ev.Peer.ID + ")");
                    break;

                case EventType.Receive:
                    byte[] payload = new byte[ev.Packet.Length];
                    ev.Packet.CopyTo(payload);
                    string text = Encoding.UTF8.GetString(payload);
                    Console.WriteLine("server: received '" + text + "' on channel " + ev.ChannelID);

                    Packet echo = new Packet();
                    echo.Create(payload, PacketFlags.Reliable);
                    Peer peer = ev.Peer;
                    peer.Send(ev.ChannelID, ref echo);

                    ev.Packet.Dispose();
                    break;

                case EventType.Disconnect:
                    Console.WriteLine("server: peer " + ev.Peer.ID + " disconnected");
                    break;

                case EventType.Timeout:
                    Console.WriteLine("server: peer " + ev.Peer.ID + " timed out");
                    break;
            }
        }

        /// <summary>
        /// Client side: dispatch one event slice. Sends one greeting on connect, watches for the echo.
        /// </summary>
        private static void PumpClient(Host client, Peer remote, ref bool connected, ref bool gotEcho)
        {
            Event ev;
            int rc = client.Service(15, out ev);
            if (rc <= 0)
                return;

            switch (ev.Type)
            {
                case EventType.Connect:
                    Console.WriteLine("client: connected, sending greeting");
                    Packet greet = new Packet();
                    greet.Create(Encoding.UTF8.GetBytes("hello from client"), PacketFlags.Reliable);
                    remote.Send(0, ref greet);
                    connected = true;
                    break;

                case EventType.Receive:
                    byte[] payload = new byte[ev.Packet.Length];
                    ev.Packet.CopyTo(payload);
                    Console.WriteLine("client: echo back -> '" + Encoding.UTF8.GetString(payload) + "'");
                    ev.Packet.Dispose();
                    gotEcho = true;
                    break;

                case EventType.Disconnect:
                    Console.WriteLine("client: server disconnected");
                    break;

                case EventType.Timeout:
                    Console.WriteLine("client: connection timed out");
                    break;
            }
        }
    }
}
