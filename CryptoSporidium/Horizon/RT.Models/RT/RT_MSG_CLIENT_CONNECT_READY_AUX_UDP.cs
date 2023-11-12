using CryptoSporidium.Horizon.RT.Common;
using CryptoSporidium.Horizon.LIBRARY.Common.Stream;

namespace CryptoSporidium.Horizon.RT.Models
{
    [ScertMessage(RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_AUX_UDP)]
    public class RT_MSG_CLIENT_CONNECT_READY_AUX_UDP : BaseScertMessage
    {
        public override RT_MSG_TYPE Id => RT_MSG_TYPE.RT_MSG_CLIENT_CONNECT_READY_AUX_UDP;

        public override void Deserialize(MessageReader reader)
        {

        }

        public override void Serialize(MessageWriter writer)
        {

        }
    }
}