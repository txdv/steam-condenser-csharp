using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Checksums;
using SteamCondenser.Steam.Packets.RCON;

namespace SteamCondenser.Steam.Packets
{
	public class SteamPacket
	{
		public static readonly byte[] Prefix = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
		public const int PACKET_SIZE = 1400;
		public const int PACKET_SPLIT_MARKER = -2;

		protected SteamPacketTypes packetType;
		protected PacketReader reader;

		public SteamPacket(SteamPacketTypes packetType)
			: this(packetType, new byte[] { })
		{
		}

		public SteamPacket(SteamPacketTypes packetType, byte[] data)
			: this(packetType, data, 0)
		{
		}

		public SteamPacket(SteamPacketTypes packetType, byte[] data, int offset)
		{
			this.packetType = packetType;
			reader = new PacketReader(data);
		}

		public virtual byte[] GetBytes()
		{
			byte[] packet = new byte[Length];
			CopyTo(packet);
			return packet;
		}

		public virtual void CopyTo(byte[] buffer)
		{
			int length = SteamPacket.Prefix.Length;
			Buffer.BlockCopy(SteamPacket.Prefix, 0, buffer, 0, length);
			buffer[length] = (byte)packetType;
			Buffer.BlockCopy(reader.Data, 0, buffer, length + 1, reader.Data.Length);
		}

		public int Length {
			get {
				return SteamPacket.Prefix.Length + 1 + Data.Length;
			}
		}

		public SteamPacketTypes PacketType {
			get {
				return this.packetType;
			}
		}

		public byte[] Data {
			get {
				return reader.Data;
			}
		}

		public static SteamPacket ReassemblePacket(List<byte[]> splitPackets)
		{
			return SteamPacket.ReassemblePacket(splitPackets, false, (short)0, 0);
		}

		public static SteamPacket ReassemblePacket(List<byte[]> splitPackets, bool isCompressed, short uncompressedSize, int packetChecksum)
		{
			byte[] packetData;
			packetData = new byte[0];
			MemoryStream memStream = new MemoryStream();
			
			foreach (byte[] splitPacket in splitPackets) {
				memStream.Write(splitPacket, 0, splitPacket.Length);
			}

			if (isCompressed) {
				BZip2InputStream bzip2 = new BZip2InputStream(new MemoryStream(packetData));
				bzip2.Read(packetData, 0, uncompressedSize);

				Crc32 crc32 = new Crc32();
				crc32.Update(packetData);

				if (crc32.Value != packetChecksum) {
					throw new Exception("CRC32 checksum mismatch of uncompressed packet data.");
				}
			}

			return SteamPacket.CreatePacket(memStream.ToArray());
		}

		public static SteamPacket CreatePacket(byte[] rawData)
		{
			
			SteamPacket packet;
			SteamPacketTypes packetType = (SteamPacketTypes)rawData[0];

			MemoryStream byteStream = new MemoryStream(rawData.Length - 1);
			byteStream.Write(rawData, 1, rawData.Length - 1);

			switch (packetType)
			{
			case SteamPacketTypes.S2C_CHALLENGE:
				packet = new ChallengeResponsePacket(byteStream.ToArray());
				break;

			case SteamPacketTypes.S2A_INFO:
				packet = new S2A_INFO_Packet(byteStream.ToArray());
				break;

			case SteamPacketTypes.S2A_INFO2:
				packet = new SourceServerInfoResponsePacket(byteStream.ToArray());
				//packet = new S2A_INFO2_Packet(byteStream.ToArray());
				break;

			case SteamPacketTypes.S2A_INFO_DETAILED:
				packet = new S2A_INFO_DETAILED_Packet(byteStream.ToArray());
				break;
			
			case SteamPacketTypes.S2A_RULES:
				packet = new ServerRulesResponsePacket(byteStream.ToArray());
				break;

			case SteamPacketTypes.S2A_PLAYER:
				packet = new PlayersResponsePacket(byteStream.ToArray());
				break;
			
			case SteamPacketTypes.RCON_GOLDSRC_CHALLENGE_HEADER:
			case SteamPacketTypes.RCON_GOLDSRC_NO_CHALLENGE_HEADER:
			case SteamPacketTypes.RCON_GOLDSRC_RESPONSE_HEADER:
				packet = new RCONGoldSrcResponsePacket(byteStream.ToArray());
				break;
			
			case SteamPacketTypes.M2A_SERVER_BATCH:
			 	packet = new MasterServerResponseBatchPacket(byteStream.ToArray());
				break;
			
			default:
				packet = new SteamPacket(packetType, byteStream.ToArray());
				break;
			}

			return packet;
		}
	}
}
