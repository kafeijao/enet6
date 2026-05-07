using System;
using System.Diagnostics;
using System.Text;

namespace ENet6.Tests
{
    /// <summary>
    /// Integration test for the C# bindings. Spins up a loopback server/client pair in a
    /// single process and exercises every public API, asserting the observable behavior.
    /// </summary>
    internal static class Program
    {
        private static int passed;
        private static int failed;

        private static int Main()
        {
            Console.WriteLine("ENet6 C# binding integration tests");
            Console.WriteLine("==================================");

            try
            {
                if (!Library.Initialize())
                {
                    Console.WriteLine("FAIL: Library.Initialize returned false");
                    return 1;
                }

                Check("Library.LinkedVersion equals managed version", Library.LinkedVersion == Library.version);

                TestTime();
                TestAddress();
                TestPacketLifecycle();
                TestRangeCoder();
                TestEndToEnd();

                Library.Deinitialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine("UNCAUGHT: " + ex);
                failed++;
            }

            Console.WriteLine();
            Console.WriteLine("Passed: " + passed + ", Failed: " + failed);
            return failed == 0 ? 0 : 1;
        }

        private static void Check(string name, bool ok)
        {
            if (ok)
            {
                passed++;
                Console.WriteLine("  ok   " + name);
            }
            else
            {
                failed++;
                Console.WriteLine("  FAIL " + name);
            }
        }

        private static void TestTime()
        {
            Console.WriteLine();
            Console.WriteLine("Library time");

            uint t1 = Library.Time;
            Library.SetTime(0);
            uint t2 = Library.Time;
            Check("Library.Time returns a value", t1 != uint.MaxValue);
            Check("Library.SetTime resets clock", t2 < 1000);
        }

        private static void TestAddress()
        {
            Console.WriteLine();
            Console.WriteLine("Address API");

            Address any4 = Address.BuildAny(AddressType.IPv4);
            Address any6 = Address.BuildAny(AddressType.IPv6);
            Check("BuildAny IPv4 type",   any4.Type == AddressType.IPv4);
            Check("BuildAny IPv4 IsAny",  any4.IsAny());
            Check("BuildAny IPv6 IsAny",  any6.IsAny());
            Check("BuildAny IPv4 not loopback",   !any4.IsLoopback());

            Address loopback4 = Address.BuildLoopback(AddressType.IPv4);
            Address loopback6 = Address.BuildLoopback(AddressType.IPv6);
            Check("BuildLoopback IPv4 IsLoopback", loopback4.IsLoopback());
            Check("BuildLoopback IPv6 IsLoopback", loopback6.IsLoopback());
            Check("Loopback IPv4 not Any",         !loopback4.IsAny());

            Address parsed = new Address();
            parsed.Type = AddressType.IPv4;
            Check("SetIP 127.0.0.1",          parsed.SetIP("127.0.0.1"));
            Check("Parsed addr is loopback",  parsed.IsLoopback());
            string roundTripNoPort = parsed.GetIP();
            Check("GetIP no-port returns 127.0.0.1", roundTripNoPort == "127.0.0.1");

            parsed.Port = 7777;
            Check("Parsed addr port", parsed.Port == 7777);
            string roundTripPort = parsed.GetIP();
            Check("GetIP with-port appends ':7777'", roundTripPort == "127.0.0.1:7777");

            byte[] hostBytes = parsed.GetHostBytes();
            Check("GetHostBytes length 16",   hostBytes.Length == 16);
            Check("GetHostBytes IPv4 stored at offset 0..3",
                hostBytes[0] == 127 && hostBytes[1] == 0 && hostBytes[2] == 0 && hostBytes[3] == 1);

            Address copy = new Address();
            copy.Type = AddressType.IPv4;
            copy.SetHostBytes(hostBytes);
            copy.Port = 7777;
            Check("EqualsHost after byte roundtrip", copy.EqualsHost(parsed));
            Check("Equals after byte roundtrip",     copy.Equals(parsed));

            Address bcast = new Address();
            bcast.Type = AddressType.IPv4;
            bcast.SetIP("255.255.255.255");
            Check("255.255.255.255 IsBroadcast",      bcast.IsBroadcast());

            Address v4 = new Address();
            v4.Type = AddressType.IPv4;
            v4.SetIP("10.0.0.1");
            v4.ConvertToIPV6();
            Check("ConvertToIPV6 changes type", v4.Type == AddressType.IPv6);
        }

        private static void TestPacketLifecycle()
        {
            Console.WriteLine();
            Console.WriteLine("Packet API");

            byte[] payload = Encoding.ASCII.GetBytes("hello-enet");
            Packet packet = new Packet();
            packet.Create(payload, PacketFlags.Reliable);
            Check("Packet IsSet after create", packet.IsSet);
            Check("Packet length matches",    packet.Length == payload.Length);

            byte[] readBack = new byte[packet.Length];
            packet.CopyTo(readBack);
            Check("Packet CopyTo roundtrip",   readBack.SequenceEqual(payload));

            Check("Packet Resize larger",     packet.Resize(payload.Length + 4));
            Check("Packet Length after resize", packet.Length == payload.Length + 4);

            packet.UserData = (IntPtr)0xCAFE;
            Check("Packet UserData roundtrip", (long)packet.UserData == 0xCAFE);

            packet.Dispose();
            Check("Packet not IsSet after dispose", !packet.IsSet);
        }

        private static void TestRangeCoder()
        {
            Console.WriteLine();
            Console.WriteLine("RangeCoder helpers");

            IntPtr ctx = RangeCoder.Create();
            Check("RangeCoder.Create non-null", ctx != IntPtr.Zero);
            RangeCoder.Destroy(ctx);
            Check("RangeCoder.Destroy ran without throwing", true);
        }

        private static void TestEndToEnd()
        {
            Console.WriteLine();
            Console.WriteLine("End-to-end client/server on loopback");

            using (Host server = new Host())
            using (Host client = new Host())
            {
                Address bind = Address.BuildAny(AddressType.IPv6);
                bind.Port = 0;
                server.Create(AddressType.IPv6, bind, 4, 2);
                Check("Server.IsSet",        server.IsSet);
                Check("Server.PeersCount 0", server.PeersCount == 0);

                client.Create(AddressType.IPv6);
                Check("Client.IsSet",        client.IsSet);

                // Discover bound port via OS: use a private socket-trick. ENet's host
                // doesn't expose its own bound port directly, so we rely on the fact that
                // BuildLoopback + a known port works once the server is started on a fixed
                // port we picked. Pick a random ephemeral port and rebind if collision.
                // For the test we simply pick a port and recreate if needed.

                server.Dispose();
                client.Dispose();
            }

            // Pick a port and create both endpoints with it.
            ushort port = (ushort)new Random().Next(40000, 60000);

            using (Host server = new Host())
            using (Host client = new Host())
            {
                Address bind = Address.BuildAny(AddressType.IPv6);
                bind.Port = port;
                server.Create(AddressType.IPv6, bind, 4, 2);
                client.Create(AddressType.IPv6);

                Address dst = Address.BuildLoopback(AddressType.IPv6);
                dst.Port = port;

                Peer clientPeer = client.Connect(dst, 2, 0xDEADBEEF);
                Check("client Connect produced peer", clientPeer.IsSet);

                Peer serverPeer = default(Peer);
                bool connected = false;
                Stopwatch watch = Stopwatch.StartNew();
                Event ev;

                while (watch.ElapsedMilliseconds < 5000 && !connected)
                {
                    int rc = server.Service(50, out ev);
                    if (rc > 0 && ev.Type == EventType.Connect)
                    {
                        serverPeer = ev.Peer;
                        connected = true;
                    }

                    rc = client.Service(50, out ev);
                    if (rc > 0 && ev.Type == EventType.Connect)
                    {
                        // client also gets a Connect event when the handshake completes
                    }
                }

                Check("Server saw Connect event", connected);
                Check("ServerPeer.State == Connected", serverPeer.State == PeerState.Connected);
                Check("ServerPeer.ID assigned",        serverPeer.ID < Library.maxPeers);
                Check("ServerPeer.MTU > 0",            serverPeer.MTU > 0);
                Check("ServerPeer.IP non-empty",       !string.IsNullOrEmpty(serverPeer.IP));

                serverPeer.ConfigureThrottle(Library.throttleInterval, Library.throttleAcceleration, Library.throttleDeceleration);
                serverPeer.Timeout(Library.timeoutLimit, Library.timeoutMinimum, Library.timeoutMaximum);
                serverPeer.PingInterval(500);
                Check("ConfigureThrottle/Timeout/PingInterval did not throw", true);

                // Send client -> server
                byte[] outbound = Encoding.ASCII.GetBytes("ping");
                Packet sendPacket = new Packet();
                sendPacket.Create(outbound, PacketFlags.Reliable);
                Check("Peer.Send returned true", clientPeer.Send(0, ref sendPacket));

                // Send server -> client (broadcast variant)
                byte[] outbound2 = Encoding.ASCII.GetBytes("pong");
                Packet broadcastPacket = new Packet();
                broadcastPacket.Create(outbound2, PacketFlags.Reliable);
                server.Broadcast(0, ref broadcastPacket);

                bool serverGotPing = false;
                bool clientGotPong = false;
                watch.Restart();
                while (watch.ElapsedMilliseconds < 5000 && (!serverGotPing || !clientGotPong))
                {
                    int rc = server.Service(50, out ev);
                    if (rc > 0 && ev.Type == EventType.Receive)
                    {
                        byte[] buf = new byte[ev.Packet.Length];
                        ev.Packet.CopyTo(buf);
                        if (Encoding.ASCII.GetString(buf) == "ping")
                            serverGotPing = true;
                        ev.Packet.Dispose();
                    }

                    rc = client.Service(50, out ev);
                    if (rc > 0 && ev.Type == EventType.Receive)
                    {
                        byte[] buf = new byte[ev.Packet.Length];
                        ev.Packet.CopyTo(buf);
                        if (Encoding.ASCII.GetString(buf) == "pong")
                            clientGotPong = true;
                        ev.Packet.Dispose();
                    }
                }

                Check("server received 'ping'", serverGotPing);
                Check("client received 'pong'", clientGotPong);

                Check("server.PacketsSent > 0",     server.PacketsSent > 0);
                Check("server.PacketsReceived > 0", server.PacketsReceived > 0);
                Check("server.BytesSent > 0",       server.BytesSent > 0);
                Check("server.BytesReceived > 0",   server.BytesReceived > 0);

                // Compressor smoke test
                server.CompressWithRangeCoder();
                client.CompressWithRangeCoder();
                Check("CompressWithRangeCoder did not throw", true);
                server.DisableCompression();
                client.DisableCompression();
                Check("DisableCompression did not throw", true);

                // Encryption disable smoke test
                server.DisableEncryption();
                client.DisableEncryption();
                Check("DisableEncryption did not throw", true);

                // Disconnect
                clientPeer.Disconnect(0);
                bool serverSawDisconnect = false;
                watch.Restart();
                while (watch.ElapsedMilliseconds < 5000 && !serverSawDisconnect)
                {
                    int rc = server.Service(50, out ev);
                    if (rc > 0 && ev.Type == EventType.Disconnect)
                        serverSawDisconnect = true;

                    client.Service(50, out ev);
                }

                Check("server saw Disconnect", serverSawDisconnect);

                server.Flush();
                client.Flush();
            }

            Check("end-to-end Host disposal did not throw", true);
        }
    }
}
