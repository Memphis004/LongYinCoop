using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LongYinCoop.S2.Protocol;

namespace LongYinCoop.S2.Transport
{
    /// <summary>
    /// TCP transport ของ S2a-3 (loopback): framing บน stream + คิวมีขอบเขต + เธรดแยก
    ///
    /// หัวใจของความปลอดภัยต่อ main thread (§1.4 / แผน §3.1):
    ///   - `Send` **ไม่บล็อก** — TryAdd ลงคิวมีขอบเขต (เต็ม = ทิ้ง + นับ) แล้วให้ writer thread เขียน
    ///   - `TryReceive` **ไม่บล็อก** — dequeue จาก inbox ที่ reader thread เติม
    ///   - IL2CPP/Unity object ไม่เคยเข้ามาในไฟล์นี้ (byte[] เท่านั้น)
    ///
    /// 1 connection ต่อครั้ง (แผน I8): ตัวที่สองถูกปฏิเสธด้วย `BusyFrame` (Deny{busy}) ถ้าตั้งไว้ แล้วปิด
    /// reconnect ได้: หลัง connection เก่าตาย listener ยังอยู่ และ `TakeNewConnection()` จะคืน true อีกครั้ง
    /// </summary>
    public sealed class TcpTransport : ITransport
    {
        public const int InboxCapacity = 256;
        public const int OutboxCapacity = 256;
        public const int ReadBufferBytes = 2048;
        public const int ConnectTimeoutMs = 2000;
        public const int JoinMs = 200;

        private readonly bool _listenerMode;
        private readonly TcpListener _listener;
        private readonly TransportStats _stats = new TransportStats();
        private readonly ConcurrentQueue<byte[]> _inbox = new ConcurrentQueue<byte[]>();

        private TcpClient _client;
        private BlockingCollection<byte[]> _outbox;
        private Thread _acceptThread;
        private Thread _readerThread;
        private Thread _writerThread;

        private volatile bool _open;
        private volatile bool _listening;
        private volatile bool _newConnection;
        private int _hasClient;
        private int _connClosed = 1;
        private int _inboxCount;
        private int _disposed;

        private TcpTransport(bool listenerMode, TcpListener listener, string bind, int port)
        {
            _listenerMode = listenerMode;
            _listener = listener;
            Bind = bind;
            Port = port;
        }

        public TransportKind Kind { get { return TransportKind.Tcp; } }

        public string Bind { get; private set; }

        public int Port { get; private set; }

        public string LastError { get; private set; }

        /// <summary>เหตุการณ์ตัดการเชื่อมต่อปกติ (guest ปิด/ตัดสาย) — ไม่ใช่ error ของ host</summary>
        public string LastDisconnect { get; private set; }

        /// <summary>frame ที่เขียนให้ client ที่สองก่อนปิด (Deny{busy}) — null = ปิดเลย</summary>
        public byte[] BusyFrame { get; set; }

        public bool IsOpen { get { return _open; } }

        public bool Listening { get { return _listening; } }

        public bool Connected { get { return _open; } }

        public TransportStats Stats { get { return _stats; } }

        public string Describe
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    _listenerMode ? "tcp-listen({0}:{1})" : "tcp({0}:{1})", Bind ?? "-", Port);
            }
        }

        // ------------------------------------------------------------------ factories

        /// <summary>เปิด listener บน loopback (bind policy ตรวจมาก่อนแล้วโดยผู้เรียก)</summary>
        public static TcpTransport Listen(string bind, int port, out string error)
        {
            error = null;
            IPAddress address;
            if (!IPAddress.TryParse(bind ?? string.Empty, out address))
            {
                error = "bind is not an IP literal";
                return null;
            }

            TcpListener listener = null;
            try
            {
                listener = new TcpListener(address, port);
                listener.Start(4);
            }
            catch (SocketException e)
            {
                error = "listen failed: " + e.SocketErrorCode;
                try { listener?.Stop(); } catch { }
                return null;
            }
            catch (Exception e)
            {
                error = "listen failed: " + e.GetType().Name;
                try { listener?.Stop(); } catch { }
                return null;
            }

            int actualPort = ((IPEndPoint)listener.LocalEndpoint).Port;
            TcpTransport transport = new TcpTransport(true, listener, bind, actualPort);
            transport.StartAcceptLoop();
            return transport;
        }

        /// <summary>ต่อเข้า host (ใช้โดย client ทดสอบ / self-test ของ mod)</summary>
        public static TcpTransport Connect(string ip, int port, out string error,
                                           int timeoutMs = ConnectTimeoutMs)
        {
            error = null;
            TcpClient client = new TcpClient();
            try
            {
                if (!client.ConnectAsync(ip, port).Wait(timeoutMs < 1 ? ConnectTimeoutMs : timeoutMs))
                {
                    error = "connect timeout";
                    try { client.Close(); } catch { }
                    return null;
                }
            }
            catch (Exception e)
            {
                error = "connect failed: " + (e.InnerException?.GetType().Name ?? e.GetType().Name);
                try { client.Close(); } catch { }
                return null;
            }

            TcpTransport transport = new TcpTransport(false, null, ip, port);
            transport.Attach(client);
            return transport;
        }

        // ------------------------------------------------------------------ connection lifecycle

        private void StartAcceptLoop()
        {
            _listening = true;
            _stats.OpenCount++;
            _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "S2TcpAccept" };
            _acceptThread.Start();
        }

        private void AcceptLoop()
        {
            while (_listening)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    if (!_listening) return;
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _hasClient, 1, 0) == 1)
                {
                    RejectSecond(client);
                    continue;
                }

                Attach(client);
            }
        }

        /// <summary>ปฏิเสธ client ที่สองแบบสุภาพ (Deny{busy}) แล้วปิด — ไม่แตะ connection ที่ใช้งานอยู่</summary>
        private void RejectSecond(TcpClient client)
        {
            _stats.RejectedConnections++;
            try
            {
                byte[] busy = BusyFrame;
                if (busy != null && busy.Length > 0)
                {
                    NetworkStream stream = client.GetStream();
                    client.SendTimeout = 500;
                    stream.Write(busy, 0, busy.Length);
                    stream.Flush();
                }
            }
            catch (Exception)
            {
                // ปฏิเสธไม่สำเร็จก็ยังต้องปิด
            }
            finally
            {
                try { client.Close(); } catch { }
            }
        }

        private void Attach(TcpClient client)
        {
            Interlocked.Exchange(ref _connClosed, 0);
            _client = client;
            try
            {
                client.NoDelay = true;
                client.ReceiveBufferSize = 64 * 1024;
            }
            catch (Exception)
            {
                // tuning ล้มเหลวไม่เป็นไร — ใช้ค่าดีฟอลต์
            }

            NetworkStream stream;
            try
            {
                stream = client.GetStream();
            }
            catch (Exception e)
            {
                LastError = "stream: " + e.GetType().Name;
                try { client.Close(); } catch { }
                Interlocked.Exchange(ref _hasClient, 0);
                return;
            }

            BlockingCollection<byte[]> outbox = new BlockingCollection<byte[]>(OutboxCapacity);
            _outbox = outbox;
            _open = true;
            _newConnection = true;
            _stats.OpenCount++;

            _readerThread = new Thread(delegate () { ReadLoop(stream); })
            { IsBackground = true, Name = "S2TcpRead" };
            _writerThread = new Thread(delegate () { WriteLoop(stream, outbox); })
            { IsBackground = true, Name = "S2TcpWrite" };
            _readerThread.Start();
            _writerThread.Start();
        }

        /// <summary>มี connection ใหม่ที่ยังไม่ได้แจ้ง main thread หรือยัง</summary>
        public bool TakeNewConnection()
        {
            if (!_newConnection) return false;
            _newConnection = false;
            return true;
        }

        private void ReadLoop(NetworkStream stream)
        {
            TcpFrameStream framing = new TcpFrameStream();
            byte[] buffer = new byte[ReadBufferBytes];
            try
            {
                while (true)
                {
                    int n = stream.Read(buffer, 0, buffer.Length);
                    if (n <= 0) break;

                    string feedError;
                    if (!framing.Feed(buffer.AsSpan(0, n), out feedError))
                    {
                        LastError = feedError;
                        break;
                    }

                    while (true)
                    {
                        byte[] frame;
                        string frameError;
                        bool got = framing.TryTakeFrame(out frame, out frameError);
                        if (frameError != null)
                        {
                            LastError = frameError;
                            return;
                        }
                        if (!got) break;
                        Enqueue(frame);
                    }
                }
            }
            catch (Exception e)
            {
                // guest ปิด/ตัดสาย = เหตุการณ์ปกติ ไม่ใช่ error ของ host
                if (e is System.IO.IOException || e is System.Net.Sockets.SocketException
                    || e is ObjectDisposedException)
                    LastDisconnect = "read: " + e.GetType().Name;
                else
                    LastError = "read: " + e.GetType().Name;
            }
            finally
            {
                CloseConnection();
            }
        }

        private void WriteLoop(NetworkStream stream, BlockingCollection<byte[]> outbox)
        {
            try
            {
                foreach (byte[] frame in outbox.GetConsumingEnumerable())
                {
                    stream.Write(frame, 0, frame.Length);
                    stream.Flush();
                    _stats.FramesSent++;
                    _stats.BytesSent += frame.Length;
                }
            }
            catch (Exception e)
            {
                if (e is System.IO.IOException || e is System.Net.Sockets.SocketException
                    || e is ObjectDisposedException)
                    LastDisconnect = "write: " + e.GetType().Name;
                else
                    LastError = "write: " + e.GetType().Name;
            }
            finally
            {
                CloseConnection();
            }
        }

        private void Enqueue(byte[] frame)
        {
            if (Interlocked.Increment(ref _inboxCount) > InboxCapacity)
            {
                Interlocked.Decrement(ref _inboxCount);
                _stats.ReceiveDrops++;
                return;
            }
            _inbox.Enqueue(frame);
        }

        private void CloseConnection()
        {
            if (Interlocked.Exchange(ref _connClosed, 1) == 1) return;
            _open = false;

            BlockingCollection<byte[]> outbox = _outbox;
            if (outbox != null)
            {
                try { outbox.CompleteAdding(); } catch (ObjectDisposedException) { }
            }
            TcpClient client = _client;
            if (client != null)
            {
                try { client.Close(); } catch { }
            }
            _stats.CloseCount++;
            Interlocked.Exchange(ref _hasClient, 0);
        }

        // ------------------------------------------------------------------ ITransport

        public void Open()
        {
            if (_listenerMode) return; // listener เปิดอยู่แล้ว
            throw new NotSupportedException("TcpTransport.Open is not supported; use Listen/Connect");
        }

        /// <summary>ปิดเฉพาะ connection ปัจจุบัน แต่ **listener ยังอยู่** (ให้ guest reconnect ได้)</summary>
        public void RequestClose()
        {
            CloseConnection();
        }

        public void Close()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _listening = false;
            try { _listener?.Stop(); } catch { }
            CloseConnection();

            JoinQuiet(_acceptThread);
            JoinQuiet(_readerThread);
            JoinQuiet(_writerThread);
        }

        private static void JoinQuiet(Thread thread)
        {
            if (thread == null) return;
            try
            {
                if (thread.IsAlive) thread.Join(JoinMs);
            }
            catch (ThreadStateException) { }
        }

        public bool Send(byte[] frame)
        {
            if (frame == null || frame.Length == 0) return false;
            BlockingCollection<byte[]> outbox = _outbox;
            if (!_open || outbox == null || outbox.IsAddingCompleted)
            {
                _stats.SendDrops++;
                return false;
            }
            if (outbox.Count >= OutboxCapacity)
            {
                _stats.SendDrops++;
                return false;
            }
            if (!outbox.TryAdd(frame))
            {
                _stats.SendDrops++;
                return false;
            }
            return true;
        }

        public bool TryReceive(out byte[] frame)
        {
            if (_inbox.TryDequeue(out frame))
            {
                Interlocked.Decrement(ref _inboxCount);
                _stats.FramesReceived++;
                _stats.BytesReceived += frame.Length;
                return true;
            }
            frame = null;
            return false;
        }

        public int Queued { get { return Volatile.Read(ref _inboxCount); } }

        public void ResetStats()
        {
            _stats.Reset();
        }
    }
}
