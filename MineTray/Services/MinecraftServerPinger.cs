using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MineTray.Models;
using DnsClient;

namespace MineTray.Services
{
    /// <summary>
    /// Minecraftサーバーにサーバーリストピング (SLP) を送信し、ステータスを取得します。
    /// </summary>
    public class MinecraftServerPinger
    {
        private const int TimeOutMs = 15000;

        /// <summary>
        /// 指定されたアドレスにPingを送信し、サーバーステータスを取得します。
        /// </summary>
        public async Task<MinecraftServerStatus?> PingAsync(string address)
        {
            return await Task.Run(async () =>
            {
                // 入力をクリーンアップ
                address = address.Trim();
                
                string connectionHost = address;
                int connectionPort = 25565;
                
                try
                {
                    // 1. ポートの解析
                    if (address.Contains(":"))
                    {
                        var parts = address.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int p))
                        {
                            connectionHost = parts[0];
                            connectionPort = p;
                        }
                    }

                    // 2. SRVレコードの検索 (ポート未指定の場合)
                    string originalHost = connectionHost;
                    
                    if (!address.Contains(":")) 
                    {
                        try 
                        {
                            var lookup = new LookupClient();
                            var result = await lookup.QueryAsync($"_minecraft._tcp.{originalHost}", QueryType.SRV);
                            if (result.Answers.SrvRecords().Any())
                            {
                                var record = result.Answers.SrvRecords().First();
                                string target = record.Target.Value.TrimEnd('.');
                                connectionHost = target;
                                connectionPort = record.Port;
                            }
                        }
                        catch (Exception ex)
                        {
                            // SRVルックアップはオプション機能。失敗してもエラーにしない
                        }
                    }

                    using var client = new TcpClient();
                    client.ReceiveBufferSize = 65535;
                    client.SendBufferSize = 65535;
                    client.ReceiveTimeout = TimeOutMs;
                    client.SendTimeout = TimeOutMs;

                    using var cts = new CancellationTokenSource(TimeOutMs);

                    try
                    {
                        await client.ConnectAsync(connectionHost, connectionPort, cts.Token);
                    }
                    catch (Exception ex)
                    {
                         return null;
                    }

                    using var networkStream = client.GetStream();

                    // --- バッチパケットの準備 (ハンドシェイク + リクエスト) ---
                    var batchBuffer = new List<byte>();

                    // 1. ハンドシェイクパケット
                    var handshakeInner = new List<byte>();
                    WriteVarInt(handshakeInner, 0x00);      // パケットID
                    WriteVarInt(handshakeInner, 763);       // プロトコルバージョン
                    WriteString(handshakeInner, originalHost); // 元のホスト名を送信
                    WriteUShort(handshakeInner, (ushort)connectionPort);
                    WriteVarInt(handshakeInner, 0x01);      // 次のステート (Status)
                    
                    WriteVarInt(batchBuffer, handshakeInner.Count);
                    batchBuffer.AddRange(handshakeInner);

                    // 2. ステータスリクエストパケット
                    var requestInner = new List<byte>();
                    WriteVarInt(requestInner, 0x00);        // パケットID (Status Request)
                    
                    WriteVarInt(batchBuffer, requestInner.Count);
                    batchBuffer.AddRange(requestInner);

                    // パケット送信
                    await networkStream.WriteAsync(batchBuffer.ToArray(), cts.Token);
                    await networkStream.FlushAsync(cts.Token);

                    // --- レスポンス読み取り ---
                    
                    // 1. パケット長を読み取り
                    int packetLength = await ReadVarIntAsync(networkStream, cts.Token);

                    if (packetLength <= 0) return null;

                    // 2. パケット本体を読み取り
                    byte[] packetBody = new byte[packetLength];
                    int totalRead = 0;
                    while (totalRead < packetLength)
                    {
                        int read = await networkStream.ReadAsync(packetBody, totalRead, packetLength - totalRead, cts.Token);
                        if (read == 0) throw new EndOfStreamException($"パケット読み取り中に接続が切断されました。期待: {packetLength}, 取得: {totalRead}");
                        totalRead += read;
                    }

                    // 3. パケット本体を解析
                    using var memStream = new MemoryStream(packetBody);
                    
                    int packetId = ReadVarInt(memStream);
                    if (packetId != 0x00) return null;

                    string json = ReadString(memStream);

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<MinecraftServerStatus>(json, options);
                }
                catch
                {
                    return null;
                }
            });
        }

        #region VarInt / パケット操作

        /// <summary>
        /// VarIntをバッファに書き込みます。
        /// </summary>
        private void WriteVarInt(List<byte> buffer, int value)
        {
            while ((value & -128) != 0)
            {
                buffer.Add((byte)(value & 127 | 128));
                value = (int)(((uint)value) >> 7);
            }
            buffer.Add((byte)value);
        }

        /// <summary>
        /// 文字列をMinecraft形式でバッファに書き込みます。
        /// </summary>
        private void WriteString(List<byte> buffer, string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            WriteVarInt(buffer, bytes.Length);
            buffer.AddRange(bytes);
        }

        /// <summary>
        /// UShortをビッグエンディアンでバッファに書き込みます。
        /// </summary>
        private void WriteUShort(List<byte> buffer, ushort value)
        {
            buffer.Add((byte)((value >> 8) & 0xFF));
            buffer.Add((byte)(value & 0xFF));
        }

        /// <summary>
        /// ストリームからVarIntを非同期で読み取ります。
        /// </summary>
        private async Task<int> ReadVarIntAsync(NetworkStream stream, CancellationToken token)
        {
            int result = 0;
            int shift = 0;
            byte[] buffer = new byte[1];
            
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, 1, token);
                if (bytesRead == 0) throw new EndOfStreamException("VarInt読み取り中に接続が切断されました");
                
                int b = buffer[0];
                result |= (b & 0x7F) << (shift * 7);
                shift++;
                
                if (shift > 5) throw new InvalidDataException("VarIntが大きすぎます");
                if ((b & 0x80) == 0) break;
            }
            return result;
        }

        /// <summary>
        /// ストリームからVarIntを同期的に読み取ります（MemoryStream用に統合）。
        /// </summary>
        private int ReadVarInt(Stream stream)
        {
            int result = 0;
            int shift = 0;
            
            while (true)
            {
                int b = stream.ReadByte();
                if (b == -1) throw new EndOfStreamException("VarInt読み取り中にストリームが終了しました");
                
                result |= (b & 0x7F) << (shift * 7);
                shift++;
                
                if (shift > 5) throw new InvalidDataException("VarIntが大きすぎます");
                if ((b & 0x80) == 0) break;
            }
            return result;
        }

        /// <summary>
        /// ストリームからMinecraft形式の文字列を読み取ります。
        /// </summary>
        private string ReadString(Stream stream)
        {
            int length = ReadVarInt(stream);
            if (length == 0) return string.Empty;

            // 不正な長さによるメモリ確保エラーを防止
            if (stream is MemoryStream ms && length > ms.Length - ms.Position) 
            {
                throw new InvalidDataException($"文字列の長さ({length})が残りのストリームサイズ({ms.Length - ms.Position})を超えています");
            }

            byte[] buffer = new byte[length];
            int totalRead = 0;
            while (totalRead < length)
            {
                int r = stream.Read(buffer, totalRead, length - totalRead);
                if (r == 0) throw new EndOfStreamException("文字列読み取り中にストリームが終了しました");
                totalRead += r;
            }
            return Encoding.UTF8.GetString(buffer);
        }

        #endregion
    }
}
