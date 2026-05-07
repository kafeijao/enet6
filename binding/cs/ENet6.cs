/*
 *  Managed C# wrapper for an extended version of ENet
 *  Copyright (c) 2013 James Bellinger
 *  Copyright (c) 2016 Nate Shoffner
 *  Copyright (c) 2018 Stanislav Denisov
 *  Copyright (c) 2023 Jérôme "SirLynix" Leclercq
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace ENet6
{
    /// <summary>
    /// Packet flag bit constants.
    ///
    /// The host must be specified in network byte-order, and the port must be in
    /// host byte-order. The constant ENET_HOST_ANY may be used to specify the
    /// default server host.
    /// </summary>
    [Flags]
    public enum PacketFlags
    {
        /// <summary>No flags.</summary>
        None = 0,
        /// <summary>Packet must be received by the target peer and resend attempts should be made until the packet is delivered.</summary>
        Reliable = 1 << 0,
        /// <summary>Packet will not be sequenced with other packets (not supported for reliable packets).</summary>
        Unsequenced = 1 << 1,
        /// <summary>Packet will not allocate data, and user must supply it instead.</summary>
        NoAllocate = 1 << 2,
        /// <summary>Packet will be fragmented using unreliable (instead of reliable) sends if it exceeds the MTU.</summary>
        UnreliableFragmented = 1 << 3,
        /// <summary>Whether the packet has been sent from all queues it has been entered into.</summary>
        Sent = 1 << 8
    }

    /// <summary>
    /// An ENet event type, as returned by <see cref="Host.Service"/> or <see cref="Host.CheckEvents"/>.
    /// </summary>
    public enum EventType
    {
        /// <summary>No event occurred within the specified time limit.</summary>
        None = 0,
        /// <summary>
        /// A connection request initiated by enet_host_connect has completed.
        /// The peer field contains the peer which successfully connected.
        /// </summary>
        Connect = 1,
        /// <summary>
        /// A peer has disconnected. This event is generated on a successful
        /// completion of a disconnect initiated by enet_peer_disconnect.
        /// The peer field contains the peer which disconnected.
        /// The data field contains user supplied data describing the disconnection,
        /// or 0, if none is available.
        /// </summary>
        Disconnect = 2,
        /// <summary>
        /// A packet has been received from a peer. The peer field specifies the
        /// peer which sent the packet. The channelID field specifies the channel
        /// number upon which the packet was received. The packet field contains
        /// the packet that was received; this packet must be destroyed with
        /// enet_packet_destroy after use.
        /// </summary>
        Receive = 3,
        /// <summary>
        /// A peer has timed out. This event is generated if a peer has timed out,
        /// or if a connection request initialized by enet_host_connect has timed out.
        /// The peer field contains the peer which disconnected. The data field
        /// contains user supplied data describing the disconnection, or 0, if none
        /// is available.
        /// </summary>
        Timeout = 4
    }

    /// <summary>
    /// State of an ENet peer in its connection lifecycle.
    /// </summary>
    public enum PeerState
    {
        /// <summary>Peer handle is null/uninitialized (C# only, not part of native enum).</summary>
        Uninitialized = -1,
        /// <summary>Peer is not connected.</summary>
        Disconnected = 0,
        /// <summary>Peer is in the process of connecting (initial connect packet sent).</summary>
        Connecting = 1,
        /// <summary>Peer is acknowledging the remote side's connect packet.</summary>
        AcknowledgingConnect = 2,
        /// <summary>Peer connection is pending acknowledgement.</summary>
        ConnectionPending = 3,
        /// <summary>Peer connection has succeeded but the connect event has not yet been dispatched.</summary>
        ConnectionSucceeded = 4,
        /// <summary>Peer is fully connected and ready to send/receive packets.</summary>
        Connected = 5,
        /// <summary>Peer will be disconnected after all queued packets are flushed.</summary>
        DisconnectLater = 6,
        /// <summary>Peer is in the process of disconnecting.</summary>
        Disconnecting = 7,
        /// <summary>Peer is acknowledging a disconnect packet.</summary>
        AcknowledgingDisconnect = 8,
        /// <summary>Peer is being recycled (zombie state).</summary>
        Zombie = 9,
        /// <summary>Peer has timed out.</summary>
        TimedOut = 10
    }

    /// <summary>
    /// Specifies the address family used by an ENet address or host.
    /// </summary>
    public enum AddressType
    {
        /// <summary>Unspecified address family.</summary>
        Any = 0,
        /// <summary>IPv4 address family.</summary>
        IPv4 = 1,
        /// <summary>IPv6 address family.</summary>
        IPv6 = 2
    }

    /// <summary>
    /// Type of socket created by enet_socket_create.
    /// </summary>
    public enum SocketType
    {
        /// <summary>Stream-oriented (TCP-like) socket.</summary>
        Stream = 1,
        /// <summary>Datagram-oriented (UDP) socket.</summary>
        Datagram = 2
    }

    /// <summary>
    /// Bit flags for enet_socket_wait describing socket-state events.
    /// </summary>
    [Flags]
    public enum SocketWait
    {
        /// <summary>No socket wait flags.</summary>
        None = 0,
        /// <summary>Socket is ready to send.</summary>
        Send = 1 << 0,
        /// <summary>Socket has data ready to receive.</summary>
        Receive = 1 << 1,
        /// <summary>Wait was interrupted.</summary>
        Interrupt = 1 << 2
    }

    /// <summary>
    /// Options that may be set on an ENet socket via enet_socket_set_option / enet_socket_get_option.
    /// </summary>
    public enum SocketOption
    {
        /// <summary>Toggle non-blocking mode.</summary>
        NonBlock = 1,
        /// <summary>Allow broadcast packets.</summary>
        Broadcast = 2,
        /// <summary>Receive buffer size in bytes.</summary>
        ReceiveBuffer = 3,
        /// <summary>Send buffer size in bytes.</summary>
        SendBuffer = 4,
        /// <summary>Allow address reuse on bind.</summary>
        ReuseAddress = 5,
        /// <summary>Receive timeout in milliseconds.</summary>
        ReceiveTimeout = 6,
        /// <summary>Send timeout in milliseconds.</summary>
        SendTimeout = 7,
        /// <summary>Retrieve and clear pending socket error.</summary>
        Error = 8,
        /// <summary>Disable Nagle's algorithm (TCP only).</summary>
        NoDelay = 9,
        /// <summary>IP TTL value.</summary>
        TTL = 10,
        /// <summary>Restrict an IPv6 socket to IPv6 only (no dual-stack).</summary>
        IPv6Only = 11
    }

    /// <summary>
    /// Direction(s) on which to shut down a socket via enet_socket_shutdown.
    /// </summary>
    public enum SocketShutdown
    {
        /// <summary>Shut down the read side.</summary>
        Read = 0,
        /// <summary>Shut down the write side.</summary>
        Write = 1,
        /// <summary>Shut down both read and write sides.</summary>
        ReadWrite = 2
    }

    /// <summary>
    /// Native ENetAddress layout (24 bytes).
    /// Host bytes occupy the union starting at offset 6 (4 bytes used for IPv4, 16 for IPv6).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct ENetAddress
    {
        [FieldOffset(0)] public AddressType type;
        [FieldOffset(4)] public ushort port;
        [FieldOffset(6)] public byte h0;
        [FieldOffset(7)] public byte h1;
        [FieldOffset(8)] public byte h2;
        [FieldOffset(9)] public byte h3;
        [FieldOffset(10)] public byte h4;
        [FieldOffset(11)] public byte h5;
        [FieldOffset(12)] public byte h6;
        [FieldOffset(13)] public byte h7;
        [FieldOffset(14)] public byte h8;
        [FieldOffset(15)] public byte h9;
        [FieldOffset(16)] public byte h10;
        [FieldOffset(17)] public byte h11;
        [FieldOffset(18)] public byte h12;
        [FieldOffset(19)] public byte h13;
        [FieldOffset(20)] public byte h14;
        [FieldOffset(21)] public byte h15;
    }

    /// <summary>
    /// Native ENetEvent layout. Should not be used directly; prefer the managed <see cref="Event"/> wrapper.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ENetEvent
    {
        public EventType type;
        public IntPtr peer;
        public byte channelID;
        public uint data;
        public IntPtr packet;
    }

    /// <summary>
    /// Native ENetCallbacks layout (custom allocators provided to enet_initialize_with_callbacks).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ENetCallbacks
    {
        public AllocCallback malloc;
        public FreeCallback free;
        public NoMemoryCallback noMemory;
    }

    /// <summary>
    /// Native ENetCompressor layout. Function pointers are populated via <see cref="Marshal.GetFunctionPointerForDelegate(Delegate)"/>.
    /// </summary>
    /// <remarks>
    /// Buffers passed to the compress/decompress callbacks point to native ENetBuffer structures whose field
    /// order differs between Windows (size_t dataLength; void * data) and Unix (void * data; size_t dataLength).
    /// Implementers should marshal these manually based on the target platform.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct ENetCompressor
    {
        /// <summary>Context data for the compressor. Must be non-NULL.</summary>
        public IntPtr context;
        /// <summary>Compresses from inBuffers[0:inBufferCount-1], containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.</summary>
        public IntPtr compress;
        /// <summary>Decompresses from inData, containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.</summary>
        public IntPtr decompress;
        /// <summary>Destroys the context when compression is disabled or the host is destroyed. May be NULL.</summary>
        public IntPtr destroy;
    }

    /// <summary>
    /// Native ENetEncryptor layout. Function pointers are populated via <see cref="Marshal.GetFunctionPointerForDelegate(Delegate)"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ENetEncryptor
    {
        /// <summary>Context data for the encryptor. Must be non-NULL.</summary>
        public IntPtr context;
        /// <summary>Encrypts from inBuffers[0:inBufferCount-1], containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.</summary>
        public IntPtr encrypt;
        /// <summary>Decrypts a packet received from peer (may be null if connection packet) from inData, containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.</summary>
        public IntPtr decrypt;
        /// <summary>Destroys the context when encryption is disabled or the host is destroyed. May be NULL.</summary>
        public IntPtr destroy;
    }

    /// <summary>
    /// Custom allocation callback. Matches ENetCallbacks::malloc.
    /// </summary>
    public delegate IntPtr AllocCallback(IntPtr size);
    /// <summary>
    /// Custom free callback. Matches ENetCallbacks::free.
    /// </summary>
    public delegate void FreeCallback(IntPtr memory);
    /// <summary>
    /// Out-of-memory notification callback. Matches ENetCallbacks::no_memory.
    /// </summary>
    public delegate void NoMemoryCallback();
    /// <summary>
    /// Callback invoked when an outgoing reliable packet has been acknowledged by the peer.
    /// </summary>
    public delegate void PacketAcknowledgeCallback(Packet packet);
    /// <summary>
    /// Callback invoked when the packet is no longer in use.
    /// </summary>
    public delegate void PacketFreeCallback(Packet packet);
    /// <summary>
    /// Callback for intercepting received raw UDP packets. Should return 1 to intercept, 0 to ignore, or -1 to propagate an error.
    /// </summary>
    public delegate int InterceptCallback(IntPtr host, IntPtr @event);
    /// <summary>
    /// Callback that computes the checksum of the data held in buffers[0:bufferCount-1].
    /// </summary>
    public delegate uint ChecksumCallback(IntPtr buffers, IntPtr bufferCount);
    /// <summary>
    /// Compresses from inBuffers[0:inBufferCount-1], containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.
    /// </summary>
    public delegate IntPtr CompressCallback(IntPtr context, IntPtr inBuffers, IntPtr inBufferCount, IntPtr inLimit, IntPtr outData, IntPtr outLimit);
    /// <summary>
    /// Decompresses from inData, containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.
    /// </summary>
    public delegate IntPtr DecompressCallback(IntPtr context, IntPtr inData, IntPtr inLimit, IntPtr outData, IntPtr outLimit);
    /// <summary>
    /// Destroys the compressor/encryptor context when the host is destroyed or compression/encryption is disabled. May be null.
    /// </summary>
    public delegate void CompressorDestroyCallback(IntPtr context);
    /// <summary>
    /// Encrypts from inBuffers[0:inBufferCount-1], containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.
    /// </summary>
    public delegate IntPtr EncryptCallback(IntPtr context, IntPtr peer, IntPtr inBuffers, IntPtr inBufferCount, IntPtr inLimit, IntPtr outData, IntPtr outLimit);
    /// <summary>
    /// Decrypts a packet received from peer (peer may be null for connection packets) from inData, containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.
    /// </summary>
    public delegate IntPtr DecryptCallback(IntPtr context, IntPtr peer, IntPtr inData, IntPtr inLimit, IntPtr outData, IntPtr outLimit);

    internal static class ArrayPool
    {
        [ThreadStatic]
        private static byte[] byteBuffer;
        [ThreadStatic]
        private static IntPtr[] pointerBuffer;

        public static byte[] GetByteBuffer()
        {
            if (byteBuffer == null)
                byteBuffer = new byte[64];

            return byteBuffer;
        }

        public static IntPtr[] GetPointerBuffer()
        {
            if (pointerBuffer == null)
                pointerBuffer = new IntPtr[Library.maxPeers];

            return pointerBuffer;
        }
    }

    /// <summary>
    /// Portable internet address structure.
    ///
    /// The host must be specified in network byte-order, and the port must be in host byte-order.
    /// </summary>
    public struct Address
    {
        private ENetAddress nativeAddress;

        internal ENetAddress NativeData
        {
            get
            {
                return nativeAddress;
            }

            set
            {
                nativeAddress = value;
            }
        }

        internal Address(ENetAddress address)
        {
            nativeAddress = address;
        }

        /// <summary>Port number in host byte-order.</summary>
        public ushort Port
        {
            get
            {
                return nativeAddress.port;
            }

            set
            {
                nativeAddress.port = value;
            }
        }

        /// <summary>Address family (IPv4, IPv6, or Any).</summary>
        public AddressType Type
        {
            get
            {
                return nativeAddress.type;
            }

            set
            {
                nativeAddress.type = value;
            }
        }

        /// <summary>
        /// Returns the raw 16-byte host portion of the address.
        /// For IPv4 addresses only the first 4 bytes are meaningful.
        /// </summary>
        public byte[] GetHostBytes()
        {
            return new byte[]
            {
                nativeAddress.h0, nativeAddress.h1, nativeAddress.h2,  nativeAddress.h3,
                nativeAddress.h4, nativeAddress.h5, nativeAddress.h6,  nativeAddress.h7,
                nativeAddress.h8, nativeAddress.h9, nativeAddress.h10, nativeAddress.h11,
                nativeAddress.h12, nativeAddress.h13, nativeAddress.h14, nativeAddress.h15
            };
        }

        /// <summary>
        /// Sets the raw host bytes. Must be 4 bytes (IPv4) or 16 bytes (IPv6); shorter buffers are zero-padded.
        /// </summary>
        public void SetHostBytes(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            if (bytes.Length != 4 && bytes.Length != 16)
                throw new ArgumentException("Host bytes must be 4 (IPv4) or 16 (IPv6) bytes long", "bytes");

            nativeAddress.h0 = bytes.Length > 0 ? bytes[0] : (byte)0;
            nativeAddress.h1 = bytes.Length > 1 ? bytes[1] : (byte)0;
            nativeAddress.h2 = bytes.Length > 2 ? bytes[2] : (byte)0;
            nativeAddress.h3 = bytes.Length > 3 ? bytes[3] : (byte)0;
            nativeAddress.h4 = bytes.Length > 4 ? bytes[4] : (byte)0;
            nativeAddress.h5 = bytes.Length > 5 ? bytes[5] : (byte)0;
            nativeAddress.h6 = bytes.Length > 6 ? bytes[6] : (byte)0;
            nativeAddress.h7 = bytes.Length > 7 ? bytes[7] : (byte)0;
            nativeAddress.h8 = bytes.Length > 8 ? bytes[8] : (byte)0;
            nativeAddress.h9 = bytes.Length > 9 ? bytes[9] : (byte)0;
            nativeAddress.h10 = bytes.Length > 10 ? bytes[10] : (byte)0;
            nativeAddress.h11 = bytes.Length > 11 ? bytes[11] : (byte)0;
            nativeAddress.h12 = bytes.Length > 12 ? bytes[12] : (byte)0;
            nativeAddress.h13 = bytes.Length > 13 ? bytes[13] : (byte)0;
            nativeAddress.h14 = bytes.Length > 14 ? bytes[14] : (byte)0;
            nativeAddress.h15 = bytes.Length > 15 ? bytes[15] : (byte)0;
        }

        /// <summary>
        /// Converts an IPv4 address (or IPv4-mapped IPv6 address) into its IPv6 representation.
        /// </summary>
        public void ConvertToIPV6()
        {
            Native.enet_address_convert_ipv6(ref nativeAddress);
        }

        /// <summary>
        /// Gives the printable form of the IP address specified in this address.
        /// </summary>
        /// <returns>Null-terminated name of the host, or an empty string on failure.</returns>
        public string GetIP()
        {
            StringBuilder ip = new StringBuilder(1025);

            if (Native.enet_address_get_host_ip(ref nativeAddress, ip, (IntPtr)ip.Capacity) != 0)
                return String.Empty;

            return ip.ToString();
        }

        /// <summary>
        /// Attempts to parse the printable form of the IP address in <paramref name="ip"/>
        /// and sets the host portion of this address if successful.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
        public bool SetIP(string ip)
        {
            if (ip == null)
                throw new ArgumentNullException("ip");

            return Native.enet_address_set_host_ip(ref nativeAddress, ip) == 0;
        }

        /// <summary>
        /// Attempts to do a reverse lookup of the host portion of this address.
        /// </summary>
        /// <returns>Null-terminated name of the host on success, or an empty string on failure.</returns>
        public string GetHost()
        {
            StringBuilder hostName = new StringBuilder(1025);

            if (Native.enet_address_get_host(ref nativeAddress, hostName, (IntPtr)hostName.Capacity) != 0)
                return String.Empty;

            return hostName.ToString();
        }

        /// <summary>
        /// Attempts to resolve the host named by <paramref name="hostName"/> and sets
        /// the host portion of this address if successful.
        /// </summary>
        /// <param name="type">Address type (any/ipv4/ipv6) to resolve to.</param>
        /// <param name="hostName">Host name to lookup.</param>
        /// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
        public bool SetHost(AddressType type, string hostName)
        {
            if (hostName == null)
                throw new ArgumentNullException("hostName");

            return Native.enet_address_set_host(ref nativeAddress, type, hostName) == 0;
        }

        /// <summary>
        /// Returns whether this address is the special "any" address for its family.
        /// </summary>
        public bool IsAny()
        {
            return Native.enet_address_is_any(ref nativeAddress) != 0;
        }

        /// <summary>
        /// Returns whether this address is the broadcast address for its family.
        /// </summary>
        public bool IsBroadcast()
        {
            return Native.enet_address_is_broadcast(ref nativeAddress) != 0;
        }

        /// <summary>
        /// Returns whether this address is a loopback address for its family.
        /// </summary>
        public bool IsLoopback()
        {
            return Native.enet_address_is_loopback(ref nativeAddress) != 0;
        }

        /// <summary>
        /// Compares the host portion of two addresses (port is ignored).
        /// </summary>
        public bool EqualsHost(Address other)
        {
            ENetAddress a = nativeAddress;
            ENetAddress b = other.nativeAddress;
            return Native.enet_address_equal_host(ref a, ref b) != 0;
        }

        /// <summary>
        /// Compares two addresses, including their port.
        /// </summary>
        public bool Equals(Address other)
        {
            ENetAddress a = nativeAddress;
            ENetAddress b = other.nativeAddress;
            return Native.enet_address_equal(ref a, ref b) != 0;
        }

        /// <summary>
        /// Builds the special "any" address for the given family (binds to all interfaces).
        /// </summary>
        public static Address BuildAny(AddressType type)
        {
            Address address = new Address();
            Native.enet_address_build_any(ref address.nativeAddress, type);

            return address;
        }

        /// <summary>
        /// Builds the loopback address for the given family.
        /// </summary>
        public static Address BuildLoopback(AddressType type)
        {
            Address address = new Address();
            Native.enet_address_build_loopback(ref address.nativeAddress, type);

            return address;
        }
    }

    /// <summary>
    /// An ENet event as returned by <see cref="Host.Service"/> or <see cref="Host.CheckEvents"/>.
    /// </summary>
    public struct Event
    {
        private ENetEvent nativeEvent;

        internal ENetEvent NativeData
        {
            get
            {
                return nativeEvent;
            }

            set
            {
                nativeEvent = value;
            }
        }

        internal Event(ENetEvent @event)
        {
            nativeEvent = @event;
        }

        /// <summary>Type of the event.</summary>
        public EventType Type
        {
            get
            {
                return nativeEvent.type;
            }
        }

        /// <summary>Peer that generated a connect, disconnect or receive event.</summary>
        public Peer Peer
        {
            get
            {
                return new Peer(nativeEvent.peer);
            }
        }

        /// <summary>Channel on the peer that generated the event, if appropriate.</summary>
        public byte ChannelID
        {
            get
            {
                return nativeEvent.channelID;
            }
        }

        /// <summary>Data associated with the event, if appropriate.</summary>
        public uint Data
        {
            get
            {
                return nativeEvent.data;
            }
        }

        /// <summary>Packet associated with the event, if appropriate.</summary>
        public Packet Packet
        {
            get
            {
                return new Packet(nativeEvent.packet);
            }
        }
    }

    /// <summary>
    /// User-overridden allocation callbacks supplied to <see cref="Library.Initialize(Callbacks)"/>.
    /// Make sure the underlying ENetCallbacks structure is zeroed out so that any additional
    /// callbacks added in future versions will be properly ignored.
    /// </summary>
    public class Callbacks
    {
        private ENetCallbacks nativeCallbacks;

        internal ENetCallbacks NativeData
        {
            get
            {
                return nativeCallbacks;
            }

            set
            {
                nativeCallbacks = value;
            }
        }

        /// <summary>
        /// Creates a new set of allocation callbacks. Any null callback will use ENet's defaults.
        /// </summary>
        public Callbacks(AllocCallback allocCallback, FreeCallback freeCallback, NoMemoryCallback noMemoryCallback)
        {
            nativeCallbacks.malloc = allocCallback;
            nativeCallbacks.free = freeCallback;
            nativeCallbacks.noMemory = noMemoryCallback;
        }
    }

    /// <summary>
    /// ENet packet structure.
    ///
    /// An ENet data packet that may be sent to or received from a peer.
    /// </summary>
    public struct Packet : IDisposable
    {
        private IntPtr nativePacket;

        internal IntPtr NativeData
        {
            get
            {
                return nativePacket;
            }

            set
            {
                nativePacket = value;
            }
        }

        internal Packet(IntPtr packet)
        {
            nativePacket = packet;
        }

        /// <summary>
        /// Decrements the packet's reference count and frees the packet if no references are remaining.
        /// </summary>
        public void Dispose()
        {
            if (nativePacket != IntPtr.Zero)
            {
                Native.enet_packet_dispose(nativePacket);
                nativePacket = IntPtr.Zero;
            }
        }

        /// <summary>Returns whether the packet has been allocated.</summary>
        public bool IsSet
        {
            get
            {
                return nativePacket != IntPtr.Zero;
            }
        }

        /// <summary>Pointer to the allocated data for the packet.</summary>
        public IntPtr Data
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_packet_get_data(nativePacket);
            }
        }

        /// <summary>Application private data, may be freely modified.</summary>
        public IntPtr UserData
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_packet_get_user_data(nativePacket);
            }

            set
            {
                ThrowIfNotCreated();

                Native.enet_packet_set_user_data(nativePacket, value);
            }
        }

        /// <summary>Length of the packet's data.</summary>
        public int Length
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_packet_get_length(nativePacket);
            }
        }

        /// <summary>Returns whether the packet is currently held by any send/receive queue.</summary>
        public bool HasReferences
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_packet_check_references(nativePacket) != 0;
            }
        }

        internal void ThrowIfNotCreated()
        {
            if (nativePacket == IntPtr.Zero)
                throw new InvalidOperationException("Packet not created");
        }

        /// <summary>
        /// Sets the function to be called when the reliable packet has been acknowledged by the peer.
        /// </summary>
        public void SetAcknowledgeCallback(IntPtr callback)
        {
            ThrowIfNotCreated();

            Native.enet_packet_set_acknowledge_callback(nativePacket, callback);
        }

        /// <summary>
        /// Sets the function to be called when the reliable packet has been acknowledged by the peer.
        /// </summary>
        public void SetAcknowledgeCallback(PacketAcknowledgeCallback callback)
        {
            ThrowIfNotCreated();

            Native.enet_packet_set_acknowledge_callback(nativePacket, Marshal.GetFunctionPointerForDelegate(callback));
        }

        /// <summary>
        /// Sets the function to be called when the packet is no longer in use.
        /// </summary>
        public void SetFreeCallback(IntPtr callback)
        {
            ThrowIfNotCreated();

            Native.enet_packet_set_free_callback(nativePacket, callback);
        }

        /// <summary>
        /// Sets the function to be called when the packet is no longer in use.
        /// </summary>
        public void SetFreeCallback(PacketFreeCallback callback)
        {
            ThrowIfNotCreated();

            Native.enet_packet_set_free_callback(nativePacket, Marshal.GetFunctionPointerForDelegate(callback));
        }

        /// <summary>
        /// Allocates an ENet packet from the supplied managed byte array.
        /// </summary>
        public void Create(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            Create(data, data.Length);
        }

        /// <summary>
        /// Allocates an ENet packet from <paramref name="length"/> bytes of <paramref name="data"/>.
        /// </summary>
        public void Create(byte[] data, int length)
        {
            Create(data, length, PacketFlags.None);
        }

        /// <summary>
        /// Allocates an ENet packet from <paramref name="data"/> with the given <paramref name="flags"/>.
        /// </summary>
        public void Create(byte[] data, PacketFlags flags)
        {
            Create(data, data.Length, flags);
        }

        /// <summary>
        /// Allocates an ENet packet from <paramref name="length"/> bytes of <paramref name="data"/>
        /// using the given <paramref name="flags"/>.
        /// </summary>
        public void Create(byte[] data, int length, PacketFlags flags)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (length < 0 || length > data.Length)
                throw new ArgumentOutOfRangeException("length");

            nativePacket = Native.enet_packet_create(data, (IntPtr)length, flags);
        }

        /// <summary>
        /// Allocates an ENet packet from a native data pointer with the given <paramref name="flags"/>.
        /// </summary>
        public void Create(IntPtr data, int length, PacketFlags flags)
        {
            if (data == IntPtr.Zero)
                throw new ArgumentNullException("data");

            if (length < 0)
                throw new ArgumentOutOfRangeException("length");

            nativePacket = Native.enet_packet_create(data, (IntPtr)length, flags);
        }

        /// <summary>
        /// Attempts to resize the data in the packet to the given <paramref name="dataLength"/>.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
        public bool Resize(int dataLength)
        {
            ThrowIfNotCreated();

            if (dataLength < 0)
                throw new ArgumentOutOfRangeException("dataLength");

            return Native.enet_packet_resize(nativePacket, (IntPtr)dataLength) == 0;
        }

        /// <summary>
        /// Copies the packet's payload into the given destination buffer.
        /// </summary>
        public void CopyTo(byte[] destination)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            Marshal.Copy(Data, destination, 0, Length);
        }
    }

    /// <summary>
    /// An ENet host for communicating with peers.
    ///
    /// No fields should be modified unless otherwise stated.
    /// </summary>
    public class Host : IDisposable
    {
        private IntPtr nativeHost;

        internal IntPtr NativeData
        {
            get
            {
                return nativeHost;
            }

            set
            {
                nativeHost = value;
            }
        }

        /// <summary>
        /// Destroys the host and all of its peers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the underlying native host. Override to add custom cleanup.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (nativeHost != IntPtr.Zero)
            {
                Native.enet_host_destroy(nativeHost);
                nativeHost = IntPtr.Zero;
            }
        }

        ~Host()
        {
            Dispose(false);
        }

        /// <summary>Returns whether the host has been created.</summary>
        public bool IsSet
        {
            get
            {
                return nativeHost != IntPtr.Zero;
            }
        }

        /// <summary>Number of currently connected peers.</summary>
        public uint PeersCount
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_host_get_peers_count(nativeHost);
            }
        }

        /// <summary>Total UDP packets sent. The user should reset to 0 as needed to prevent overflow.</summary>
        public uint PacketsSent
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_host_get_packets_sent(nativeHost);
            }
        }

        /// <summary>Total UDP packets received. The user should reset to 0 as needed to prevent overflow.</summary>
        public uint PacketsReceived
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_host_get_packets_received(nativeHost);
            }
        }

        /// <summary>Total bytes sent. The user should reset to 0 as needed to prevent overflow.</summary>
        public uint BytesSent
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_host_get_bytes_sent(nativeHost);
            }
        }

        /// <summary>Total bytes received. The user should reset to 0 as needed to prevent overflow.</summary>
        public uint BytesReceived
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_host_get_bytes_received(nativeHost);
            }
        }

        internal void ThrowIfNotCreated()
        {
            if (nativeHost == IntPtr.Zero)
                throw new InvalidOperationException("Host not created");
        }

        private static void ThrowIfChannelsExceeded(int channelLimit)
        {
            if (channelLimit < 0 || channelLimit > Library.maxChannelCount)
                throw new ArgumentOutOfRangeException("channelLimit");
        }

        /// <summary>Creates a client host with default settings (1 peer, no bandwidth limits).</summary>
        public void Create(AddressType addressType)
        {
            Create(addressType, null, 1, 0, 0, 0);
        }

        /// <summary>Creates a host bound to <paramref name="address"/> with the given peer limit.</summary>
        public void Create(AddressType addressType, Address? address, int peerLimit)
        {
            Create(addressType, address, peerLimit, 0, 0, 0);
        }

        /// <summary>Creates a host bound to <paramref name="address"/> with the given peer and channel limits.</summary>
        public void Create(AddressType addressType, Address? address, int peerLimit, int channelLimit)
        {
            Create(addressType, address, peerLimit, channelLimit, 0, 0);
        }

        /// <summary>Creates a client-style host with no bound address.</summary>
        public void Create(AddressType addressType, int peerLimit, int channelLimit)
        {
            Create(addressType, null, peerLimit, channelLimit, 0, 0);
        }

        /// <summary>Creates a client-style host with bandwidth limits.</summary>
        public void Create(AddressType addressType, int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth)
        {
            Create(addressType, null, peerLimit, channelLimit, incomingBandwidth, outgoingBandwidth);
        }

        /// <summary>
        /// Creates an ENet host. Pass <c>null</c> for <paramref name="address"/> to create a client.
        /// </summary>
        /// <param name="addressType">Address family for the underlying socket.</param>
        /// <param name="address">Bound address; clients should pass <c>null</c>.</param>
        /// <param name="peerLimit">Maximum number of peers; clamped to <see cref="Library.maxPeers"/>.</param>
        /// <param name="channelLimit">Maximum number of channels per peer (0 to use ENet's default).</param>
        /// <param name="incomingBandwidth">Downstream bandwidth limit in bytes/second (0 for unlimited).</param>
        /// <param name="outgoingBandwidth">Upstream bandwidth limit in bytes/second (0 for unlimited).</param>
        public void Create(AddressType addressType, Address? address, int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth)
        {
            if (nativeHost != IntPtr.Zero)
                throw new InvalidOperationException("Host already created");

            if (peerLimit < 0 || peerLimit > Library.maxPeers)
                throw new ArgumentOutOfRangeException("peerLimit");

            ThrowIfChannelsExceeded(channelLimit);

            if (address != null)
            {
                var nativeAddress = address.Value.NativeData;

                nativeHost = Native.enet_host_create(addressType, ref nativeAddress, (IntPtr)peerLimit, (IntPtr)channelLimit, incomingBandwidth, outgoingBandwidth);
            }
            else
            {
                nativeHost = Native.enet_host_create(addressType, IntPtr.Zero, (IntPtr)peerLimit, (IntPtr)channelLimit, incomingBandwidth, outgoingBandwidth);
            }

            if (nativeHost == IntPtr.Zero)
                throw new InvalidOperationException("Host creation call failed");
        }

        /// <summary>
        /// Queues a packet to be sent to all peers associated with the host.
        /// </summary>
        public void Broadcast(byte channelID, ref Packet packet)
        {
            ThrowIfNotCreated();

            packet.ThrowIfNotCreated();
            Native.enet_host_broadcast(nativeHost, channelID, packet.NativeData);
            packet.NativeData = IntPtr.Zero;
        }

        /// <summary>
        /// Checks for any queued events on the host and dispatches one if available.
        /// </summary>
        /// <returns>&gt; 0 if an event occurred, 0 if no events available, &lt; 0 on failure.</returns>
        public int CheckEvents(out Event @event)
        {
            ThrowIfNotCreated();

            ENetEvent nativeEvent;

            var result = Native.enet_host_check_events(nativeHost, out nativeEvent);

            if (result <= 0)
            {
                @event = default(Event);

                return result;
            }

            @event = new Event(nativeEvent);

            return result;
        }

        /// <summary>
        /// Initiates a connection to a foreign host on the default channel limit.
        /// </summary>
        public Peer Connect(Address address)
        {
            return Connect(address, 0, 0);
        }

        /// <summary>
        /// Initiates a connection to a foreign host with the given channel limit.
        /// </summary>
        public Peer Connect(Address address, int channelLimit)
        {
            return Connect(address, channelLimit, 0);
        }

        /// <summary>
        /// Initiates a connection to a foreign host.
        /// </summary>
        /// <param name="address">Destination address.</param>
        /// <param name="channelLimit">Number of channels to allocate (0 to use the host default).</param>
        /// <param name="data">Integer value passed to the connecting peer's connect event.</param>
        public Peer Connect(Address address, int channelLimit, uint data)
        {
            ThrowIfNotCreated();
            ThrowIfChannelsExceeded(channelLimit);

            var nativeAddress = address.NativeData;
            var peer = new Peer(Native.enet_host_connect(nativeHost, ref nativeAddress, (IntPtr)channelLimit, data));

            if (peer.NativeData == IntPtr.Zero)
                throw new InvalidOperationException("Host connect call failed");

            return peer;
        }

        /// <summary>
        /// Waits for events on the host and shuttles packets between the host and its peers.
        /// </summary>
        /// <param name="timeout">Number of milliseconds to wait for an event.</param>
        /// <param name="event">Receives the dispatched event, if any.</param>
        /// <returns>&gt; 0 if an event occurred, 0 if no events available, &lt; 0 on failure.</returns>
        public int Service(int timeout, out Event @event)
        {
            if (timeout < 0)
                throw new ArgumentOutOfRangeException("timeout");

            ThrowIfNotCreated();

            ENetEvent nativeEvent;

            var result = Native.enet_host_service(nativeHost, out nativeEvent, (uint)timeout);

            if (result <= 0)
            {
                @event = default(Event);

                return result;
            }

            @event = new Event(nativeEvent);

            return result;
        }

        /// <summary>
        /// Adjusts the bandwidth limits of a host. Both values are in bytes/second; 0 means unlimited.
        /// </summary>
        public void SetBandwidthLimit(uint incomingBandwidth, uint outgoingBandwidth)
        {
            ThrowIfNotCreated();

            Native.enet_host_bandwidth_limit(nativeHost, incomingBandwidth, outgoingBandwidth);
        }

        /// <summary>
        /// Limits the maximum allowed channels of future incoming connections.
        /// </summary>
        public void SetChannelLimit(int channelLimit)
        {
            ThrowIfNotCreated();
            ThrowIfChannelsExceeded(channelLimit);

            Native.enet_host_channel_limit(nativeHost, (IntPtr)channelLimit);
        }

        /// <summary>
        /// Sets the optional number of allowed peers from duplicate IPs (defaults to ENET_PROTOCOL_MAXIMUM_PEER_ID).
        /// </summary>
        public void SetMaxDuplicatePeers(ushort number)
        {
            ThrowIfNotCreated();

            Native.enet_host_set_max_duplicate_peers(nativeHost, number);
        }

        /// <summary>
        /// Sets the callback used to intercept received raw UDP packets.
        /// </summary>
        public void SetInterceptCallback(IntPtr callback)
        {
            ThrowIfNotCreated();

            Native.enet_host_set_intercept_callback(nativeHost, callback);
        }

        /// <summary>
        /// Sets the callback used to intercept received raw UDP packets.
        /// </summary>
        public void SetInterceptCallback(InterceptCallback callback)
        {
            ThrowIfNotCreated();

            Native.enet_host_set_intercept_callback(nativeHost, Marshal.GetFunctionPointerForDelegate(callback));
        }

        /// <summary>
        /// Sets the callback used to compute packet checksums.
        /// </summary>
        public void SetChecksumCallback(IntPtr callback)
        {
            ThrowIfNotCreated();

            Native.enet_host_set_checksum_callback(nativeHost, callback);
        }

        /// <summary>
        /// Sets the callback used to compute packet checksums.
        /// </summary>
        public void SetChecksumCallback(ChecksumCallback callback)
        {
            ThrowIfNotCreated();

            Native.enet_host_set_checksum_callback(nativeHost, Marshal.GetFunctionPointerForDelegate(callback));
        }

        /// <summary>
        /// Sets the packet compressor the host should use to compress and decompress packets.
        /// Pass an empty/zeroed compressor to disable compression.
        /// </summary>
        public void SetCompressor(ENetCompressor compressor)
        {
            ThrowIfNotCreated();

            Native.enet_host_compress(nativeHost, ref compressor);
        }

        /// <summary>
        /// Disables packet compression on this host.
        /// </summary>
        public void DisableCompression()
        {
            ThrowIfNotCreated();

            Native.enet_host_compress(nativeHost, IntPtr.Zero);
        }

        /// <summary>
        /// Sets the packet compressor the host should use to use the default range coder.
        /// </summary>
        /// <returns>0 on success, &lt; 0 on failure.</returns>
        public int CompressWithRangeCoder()
        {
            ThrowIfNotCreated();

            return Native.enet_host_compress_with_range_coder(nativeHost);
        }

        /// <summary>
        /// Sets the packet encryptor the host should use to encrypt and decrypt packets.
        /// Pass an empty/zeroed encryptor to disable encryption.
        /// </summary>
        public void SetEncryptor(ENetEncryptor encryptor)
        {
            ThrowIfNotCreated();

            Native.enet_host_encrypt(nativeHost, ref encryptor);
        }

        /// <summary>
        /// Disables packet encryption on this host.
        /// </summary>
        public void DisableEncryption()
        {
            ThrowIfNotCreated();

            Native.enet_host_encrypt(nativeHost, IntPtr.Zero);
        }

        /// <summary>
        /// Sends any queued packets on the host specified to its designated peers.
        /// </summary>
        public void Flush()
        {
            ThrowIfNotCreated();

            Native.enet_host_flush(nativeHost);
        }
    }

    /// <summary>
    /// An ENet peer which data packets may be sent or received from.
    ///
    /// No fields should be modified unless otherwise specified.
    /// </summary>
    public struct Peer
    {
        private IntPtr nativePeer;
        private uint nativeID;

        internal IntPtr NativeData
        {
            get
            {
                return nativePeer;
            }

            set
            {
                nativePeer = value;
            }
        }

        internal Peer(IntPtr peer)
        {
            nativePeer = peer;
            nativeID = nativePeer != IntPtr.Zero ? Native.enet_peer_get_id(nativePeer) : 0;
        }

        /// <summary>Returns whether the peer handle is non-null.</summary>
        public bool IsSet
        {
            get
            {
                return nativePeer != IntPtr.Zero;
            }
        }

        /// <summary>Internal peer ID, fixed for the lifetime of the host.</summary>
        public uint ID
        {
            get
            {
                return nativeID;
            }
        }

        /// <summary>Printable form of the peer's IP address.</summary>
        public string IP
        {
            get
            {
                ThrowIfNotCreated();

                byte[] ip = ArrayPool.GetByteBuffer();

                if (Native.enet_peer_get_ip(nativePeer, ip, (IntPtr)ip.Length) == 0)
                    return Encoding.ASCII.GetString(ip, 0, ip.StringLength());
                else
                    return String.Empty;
            }
        }

        /// <summary>Peer port number in host byte-order.</summary>
        public ushort Port
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_peer_get_port(nativePeer);
            }
        }

        /// <summary>Maximum transmission unit currently negotiated for the peer.</summary>
        public uint MTU
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_peer_get_mtu(nativePeer);
            }
        }

        /// <summary>Current connection state of the peer.</summary>
        public PeerState State
        {
            get
            {
                return nativePeer == IntPtr.Zero ? PeerState.Uninitialized : Native.enet_peer_get_state(nativePeer);
            }
        }

        /// <summary>
        /// Mean round trip time (RTT), in milliseconds, between sending a reliable packet and receiving its acknowledgement.
        /// </summary>
        public uint RoundTripTime
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_peer_get_rtt(nativePeer);
            }
        }

        /// <summary>Last individually measured round trip time, in milliseconds.</summary>
        public uint LastRoundTripTime
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_peer_get_last_rtt(nativePeer);
            }
        }

        /// <summary>ENet timestamp at which the peer last sent data.</summary>
        public uint LastSendTime
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_peer_get_lastsendtime(nativePeer);
            }
        }

        /// <summary>ENet timestamp at which the peer last received data.</summary>
        public uint LastReceiveTime
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_peer_get_lastreceivetime(nativePeer);
            }
        }

        /// <summary>
        /// Current packet throttle ratio in [0, 1].
        /// </summary>
        public float PacketsThrottle
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_peer_get_packets_throttle(nativePeer);
            }
        }

        /// <summary>Application private data, may be freely modified.</summary>
        public IntPtr Data
        {
            get
            {
                ThrowIfNotCreated();

                return Native.enet_peer_get_data(nativePeer);
            }

            set
            {
                ThrowIfNotCreated();

                Native.enet_peer_set_data(nativePeer, value);
            }
        }

        internal void ThrowIfNotCreated()
        {
            if (nativePeer == IntPtr.Zero)
                throw new InvalidOperationException("Peer not created");
        }

        /// <summary>
        /// Configures throttle parameter for a peer.
        ///
        /// Unreliable packets are dropped by ENet in response to the varying conditions of the Internet connection
        /// to the peer. The throttle represents a probability that an unreliable packet should not be dropped and
        /// thus sent by ENet to the peer. The lowest mean round trip time from the sending of a reliable packet
        /// to the receipt of its acknowledgement is measured over an amount of time specified by <paramref name="interval"/>.
        /// </summary>
        /// <param name="interval">Interval, in milliseconds, over which to measure lowest mean RTT.</param>
        /// <param name="acceleration">Rate at which to increase the throttle probability as mean RTT declines.</param>
        /// <param name="deceleration">Rate at which to decrease the throttle probability as mean RTT increases.</param>
        public void ConfigureThrottle(uint interval, uint acceleration, uint deceleration)
        {
            ThrowIfNotCreated();

            Native.enet_peer_throttle_configure(nativePeer, interval, acceleration, deceleration);
        }

        /// <summary>
        /// Queues a packet to be sent to a peer.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
        public bool Send(byte channelID, ref Packet packet)
        {
            ThrowIfNotCreated();

            packet.ThrowIfNotCreated();

            return Native.enet_peer_send(nativePeer, channelID, packet.NativeData) == 0;
        }

        /// <summary>
        /// Attempts to dequeue any incoming queued packet.
        /// </summary>
        public bool Receive(out byte channelID, out Packet packet)
        {
            ThrowIfNotCreated();

            IntPtr nativePacket = Native.enet_peer_receive(nativePeer, out channelID);

            if (nativePacket != IntPtr.Zero)
            {
                packet = new Packet(nativePacket);

                return true;
            }

            packet = default(Packet);

            return false;
        }

        /// <summary>
        /// Sends a ping request to a peer.
        /// </summary>
        public void Ping()
        {
            ThrowIfNotCreated();

            Native.enet_peer_ping(nativePeer);
        }

        /// <summary>
        /// Sets the interval at which pings will be sent to a peer.
        /// </summary>
        /// <param name="interval">Interval, in milliseconds (0 to use ENet's default of 500 ms).</param>
        public void PingInterval(uint interval)
        {
            ThrowIfNotCreated();

            Native.enet_peer_ping_interval(nativePeer, interval);
        }

        /// <summary>
        /// Sets the timeout parameters for a peer.
        ///
        /// The timeout parameters control how and when a peer will timeout from a failure to acknowledge reliable
        /// traffic. Timeout values use an exponential backoff mechanism, where if a reliable packet is not
        /// acknowledged within some multiple of the average RTT plus a variance tolerance, the timeout will be
        /// doubled until it reaches a set limit.
        /// </summary>
        public void Timeout(uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum)
        {
            ThrowIfNotCreated();

            Native.enet_peer_timeout(nativePeer, timeoutLimit, timeoutMinimum, timeoutMaximum);
        }

        /// <summary>
        /// Requests a disconnection from a peer.
        /// </summary>
        public void Disconnect(uint data)
        {
            ThrowIfNotCreated();

            Native.enet_peer_disconnect(nativePeer, data);
        }

        /// <summary>
        /// Forcefully disconnects a peer immediately. The foreign host represented by the peer is not notified.
        /// </summary>
        public void DisconnectNow(uint data)
        {
            ThrowIfNotCreated();

            Native.enet_peer_disconnect_now(nativePeer, data);
        }

        /// <summary>
        /// Requests a disconnection from a peer, but only after all queued outgoing packets are sent.
        /// </summary>
        public void DisconnectLater(uint data)
        {
            ThrowIfNotCreated();

            Native.enet_peer_disconnect_later(nativePeer, data);
        }

        /// <summary>
        /// Forcefully disconnects a peer. The foreign host represented by the peer is not notified of the
        /// disconnection and no event is generated.
        /// </summary>
        public void Reset()
        {
            ThrowIfNotCreated();

            Native.enet_peer_reset(nativePeer);
        }
    }

    /// <summary>
    /// Helper extensions used by the binding.
    /// </summary>
    public static class Extensions
    {
        /// <summary>Returns the index of the first NUL byte (or the buffer length if none).</summary>
        public static int StringLength(this byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            int i;

            for (i = 0; i < data.Length && data[i] != 0; i++) ;

            return i;
        }
    }

    /// <summary>
    /// Library-wide constants and global functions (initialization, time, version).
    /// </summary>
    public static class Library
    {
        /// <summary>Maximum allowed channel count per peer.</summary>
        public const uint maxChannelCount = 0xFF;
        /// <summary>Maximum allowed peer count for a single host.</summary>
        public const uint maxPeers = 0xFFF;
        /// <summary>Default maximum packet size (32 MiB).</summary>
        public const uint maxPacketSize = 32 * 1024 * 1024;
        /// <summary>Default packet throttle scale.</summary>
        public const uint throttleScale = 32;
        /// <summary>Default packet throttle acceleration.</summary>
        public const uint throttleAcceleration = 2;
        /// <summary>Default packet throttle deceleration.</summary>
        public const uint throttleDeceleration = 2;
        /// <summary>Default packet throttle interval, in milliseconds.</summary>
        public const uint throttleInterval = 5000;
        /// <summary>Default peer timeout limit (number of retransmissions).</summary>
        public const uint timeoutLimit = 32;
        /// <summary>Default minimum timeout, in milliseconds.</summary>
        public const uint timeoutMinimum = 5000;
        /// <summary>Default maximum timeout, in milliseconds.</summary>
        public const uint timeoutMaximum = 30000;
        /// <summary>Encoded ENet library version (must match the native library version).</summary>
        public const uint version = (6 << 16) | (1 << 8) | (3);

        /// <summary>
        /// Returns the wall-time, in milliseconds. Its initial value is unspecified unless set via <see cref="SetTime"/>.
        /// </summary>
        public static uint Time
        {
            get
            {
                return Native.enet_time_get();
            }
        }

        /// <summary>
        /// Sets the current wall-time, in milliseconds.
        /// </summary>
        public static void SetTime(uint newTimeBase)
        {
            Native.enet_time_set(newTimeBase);
        }

        /// <summary>
        /// Initializes ENet globally. Must be called prior to using any functions in ENet.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
        public static bool Initialize()
        {
            if (Native.enet_linked_version() != version)
                throw new InvalidOperationException("Incompatible version");

            return Native.enet_initialize() == 0;
        }

        /// <summary>
        /// Initializes ENet globally and supplies user-overridden callbacks. Must be called prior to using any
        /// functions in ENet. Do not use <see cref="Initialize()"/> if you use this variant.
        /// </summary>
        /// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
        public static bool Initialize(Callbacks callbacks)
        {
            if (callbacks == null)
                throw new ArgumentNullException("callbacks");

            if (Native.enet_linked_version() != version)
                throw new InvalidOperationException("Incompatible version");

            ENetCallbacks nativeCallbacks = callbacks.NativeData;

            return Native.enet_initialize_with_callbacks(version, ref nativeCallbacks) == 0;
        }

        /// <summary>
        /// Shuts down ENet globally. Should be called when a program that has initialized ENet exits.
        /// </summary>
        public static void Deinitialize()
        {
            Native.enet_deinitialize();
        }

        /// <summary>
        /// Returns the linked native ENet library version (encoded as MAJOR.MINOR.PATCH).
        /// </summary>
        public static uint LinkedVersion
        {
            get
            {
                return Native.enet_linked_version();
            }
        }

        /// <summary>
        /// Computes the CRC32 of the data held in <paramref name="buffers"/>[0:bufferCount-1].
        /// </summary>
        public static uint CRC32(IntPtr buffers, int bufferCount)
        {
            return Native.enet_crc32(buffers, (IntPtr)bufferCount);
        }
    }

    /// <summary>
    /// Standalone range coder helpers exposed by ENet for custom plumbing.
    /// </summary>
    public static class RangeCoder
    {
        /// <summary>Allocates and returns a new range coder context.</summary>
        public static IntPtr Create()
        {
            return Native.enet_range_coder_create();
        }

        /// <summary>Frees a range coder context previously returned by <see cref="Create"/>.</summary>
        public static void Destroy(IntPtr context)
        {
            Native.enet_range_coder_destroy(context);
        }

        /// <summary>
        /// Compresses from <paramref name="inBuffers"/>[0:inBufferCount-1], containing <paramref name="inLimit"/> bytes,
        /// to <paramref name="outData"/>, outputting at most <paramref name="outLimit"/> bytes.
        /// </summary>
        /// <returns>Number of bytes written, or 0 on failure.</returns>
        public static IntPtr Compress(IntPtr context, IntPtr inBuffers, int inBufferCount, int inLimit, IntPtr outData, int outLimit)
        {
            return Native.enet_range_coder_compress(context, inBuffers, (IntPtr)inBufferCount, (IntPtr)inLimit, outData, (IntPtr)outLimit);
        }

        /// <summary>
        /// Decompresses from <paramref name="inData"/>, containing <paramref name="inLimit"/> bytes, to
        /// <paramref name="outData"/>, outputting at most <paramref name="outLimit"/> bytes.
        /// </summary>
        /// <returns>Number of bytes written, or 0 on failure.</returns>
        public static IntPtr Decompress(IntPtr context, IntPtr inData, int inLimit, IntPtr outData, int outLimit)
        {
            return Native.enet_range_coder_decompress(context, inData, (IntPtr)inLimit, outData, (IntPtr)outLimit);
        }
    }

    [SuppressUnmanagedCodeSecurity]
    internal static class Native
    {
#if __IOS__ || (UNITY_IOS && !UNITY_EDITOR)
        private const string nativeLibrary = "__Internal";
#else
        private const string nativeLibrary = "enet6";
#endif

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_initialize();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_initialize_with_callbacks(uint version, ref ENetCallbacks inits);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_deinitialize();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_linked_version();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_time_get();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_time_set(uint newTimeBase);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_crc32(IntPtr buffers, IntPtr bufferCount);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_set_host_ip(ref ENetAddress address, string ip);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_set_host(ref ENetAddress address, AddressType type, string hostName);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_get_host_ip(ref ENetAddress address, StringBuilder ip, IntPtr ipLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_get_host(ref ENetAddress address, StringBuilder hostName, IntPtr nameLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_address_build_any(ref ENetAddress address, AddressType type);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_address_build_loopback(ref ENetAddress address, AddressType type);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_address_convert_ipv6(ref ENetAddress address);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_equal_host(ref ENetAddress firstAddress, ref ENetAddress secondAddress);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_equal(ref ENetAddress firstAddress, ref ENetAddress secondAddress);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_is_any(ref ENetAddress address);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_is_broadcast(ref ENetAddress address);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_is_loopback(ref ENetAddress address);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create(byte[] data, IntPtr dataLength, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create(IntPtr data, IntPtr dataLength, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_packet_resize(IntPtr packet, IntPtr dataLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_packet_check_references(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_get_data(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_get_user_data(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_packet_set_user_data(IntPtr packet, IntPtr userData);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_packet_get_length(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_packet_set_acknowledge_callback(IntPtr packet, IntPtr callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_packet_set_free_callback(IntPtr packet, IntPtr callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_packet_dispose(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_create(AddressType addressType, ref ENetAddress address, IntPtr peerLimit, IntPtr channelLimit, uint incomingBandwidth, uint outgoingBandwidth);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_create(AddressType addressType, IntPtr address, IntPtr peerLimit, IntPtr channelLimit, uint incomingBandwidth, uint outgoingBandwidth);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_connect(IntPtr host, ref ENetAddress address, IntPtr channelCount, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_broadcast(IntPtr host, byte channelID, IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_service(IntPtr host, out ENetEvent @event, uint timeout);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_check_events(IntPtr host, out ENetEvent @event);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_channel_limit(IntPtr host, IntPtr channelLimit);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_bandwidth_limit(IntPtr host, uint incomingBandwidth, uint outgoingBandwidth);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_peers_count(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_packets_sent(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_packets_received(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_bytes_sent(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_bytes_received(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_set_max_duplicate_peers(IntPtr host, ushort number);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_set_intercept_callback(IntPtr host, IntPtr callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_set_checksum_callback(IntPtr host, IntPtr callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_compress(IntPtr host, ref ENetCompressor compressor);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "enet_host_compress")]
        internal static extern void enet_host_compress(IntPtr host, IntPtr compressor);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_compress_with_range_coder(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_encrypt(IntPtr host, ref ENetEncryptor encryptor);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "enet_host_encrypt")]
        internal static extern void enet_host_encrypt(IntPtr host, IntPtr encryptor);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_flush(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_destroy(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_throttle_configure(IntPtr peer, uint interval, uint acceleration, uint deceleration);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_id(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_peer_get_ip(IntPtr peer, byte[] ip, IntPtr ipLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort enet_peer_get_port(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_mtu(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern PeerState enet_peer_get_state(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_rtt(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_last_rtt(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_lastsendtime(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_lastreceivetime(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern float enet_peer_get_packets_throttle(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_peer_get_data(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_set_data(IntPtr peer, IntPtr data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_peer_send(IntPtr peer, byte channelID, IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_peer_receive(IntPtr peer, out byte channelID);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_ping(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_ping_interval(IntPtr peer, uint pingInterval);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_timeout(IntPtr peer, uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect_now(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect_later(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_reset(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_range_coder_create();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_range_coder_destroy(IntPtr context);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_range_coder_compress(IntPtr context, IntPtr inBuffers, IntPtr inBufferCount, IntPtr inLimit, IntPtr outData, IntPtr outLimit);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_range_coder_decompress(IntPtr context, IntPtr inData, IntPtr inLimit, IntPtr outData, IntPtr outLimit);
    }
}