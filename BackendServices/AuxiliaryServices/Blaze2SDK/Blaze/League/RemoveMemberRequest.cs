using Tdf;

namespace Blaze2SDK.Blaze.League
{
    [TdfStruct]
    public struct RemoveMemberRequest
    {
        
        [TdfMember("LGID")]
        public uint mLeagueId;
        
        [TdfMember("MMBR")]
        public uint mMemberId;
        
    }
}
