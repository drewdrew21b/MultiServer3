using PSMultiServer.SRC_Addons.MEDIUS.RT.Common;
using PSMultiServer.SRC_Addons.MEDIUS.Server.Common.Stream;

namespace PSMultiServer.SRC_Addons.MEDIUS.RT.Models
{
    [MediusMessage(NetMessageClass.MessageClassDME, MediusDmeMessageIds.SMMigrateOrder)]
    public class TypeSMMigrateOrder : BaseDMEMessage
    {

        public override byte PacketType => (byte)MediusDmeMessageIds.SMMigrateOrder;

        public long MigrateOrder;

        public override void Deserialize(MessageReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            MigrateOrder = reader.ReadUInt32();
        }

        public override void Serialize(MessageWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(MigrateOrder);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
                $"MigrateOrder: {MigrateOrder}";
        }
    }
}