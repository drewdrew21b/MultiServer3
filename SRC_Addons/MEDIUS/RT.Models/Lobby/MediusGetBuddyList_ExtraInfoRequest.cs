using PSMultiServer.SRC_Addons.MEDIUS.RT.Common;
using PSMultiServer.SRC_Addons.MEDIUS.Server.Common.Stream;

namespace PSMultiServer.SRC_Addons.MEDIUS.RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassLobby, MediusLobbyMessageIds.GetBuddyList_ExtraInfo)]
    public class MediusGetBuddyList_ExtraInfoRequest : BaseLobbyMessage, IMediusRequest
    {
        public override byte PacketType => (byte)MediusLobbyMessageIds.GetBuddyList_ExtraInfo;

        public MessageId MessageID { get; set; }

        public override void Deserialize(MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            //
            MessageID = reader.Read<MessageId>();
        }

        public override void Serialize(MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            //
            writer.Write(MessageID ?? MessageId.Empty);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                            $"MessageID:{MessageID}";
        }
    }
}