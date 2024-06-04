using System.ComponentModel.DataAnnotations;
using Tdf;

namespace Blaze2SDK.Blaze.Locker
{
    [TdfStruct]
    public struct CreateSubContentInfoRequest
    {
        
        /// <summary>
        /// Max String Length: 32
        /// </summary>
        [TdfMember("CCAT")]
        [StringLength(32)]
        public string mContentCategory;
        
        [TdfMember("CID")]
        public int mContentId;
        
        /// <summary>
        /// Max String Length: 32
        /// </summary>
        [TdfMember("SUBL")]
        public List<string> mSubContentNames;
        
    }
}
