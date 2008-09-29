using System;
using System.Collections.Generic;
using System.Text;

namespace SteamCondensor.Steam.Packets
{
	class GameServerQueryRequestPacket : SteamPacket
	{
		public GameServerQueryRequestPacket(SteamPacketTypes requestType)
			: base(requestType)
		{
			
		}

		public GameServerQueryRequestPacket(SteamPacketTypes requestType, byte[] data)
			: base(requestType, data)
		{

		}
	}
}
