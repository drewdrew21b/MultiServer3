using Tdf;

namespace Blaze2SDK.Blaze.GameManager
{
    [TdfStruct]
    public struct NotifyQueueChanged
    {
        
        [TdfMember("GID")]
        public uint mGameId;
        
        [TdfMember("PIDL")]
        public List<uint> mPlayerIdList;
        
    }
}
