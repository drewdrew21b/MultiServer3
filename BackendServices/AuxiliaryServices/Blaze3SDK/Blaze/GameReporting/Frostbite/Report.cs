using Tdf;

namespace Blaze3SDK.Blaze.GameReporting.Frostbite
{
	[TdfStruct(0x7812B556)]
	public struct Report
	{

		[TdfMember("GAME")]
		public SortedDictionary<string, string> mGameAttributes;

		[TdfMember("CLBS")]
		public SortedDictionary<uint, GroupReport> mGroupReports;

		[TdfMember("PLYR")]
		public SortedDictionary<long, EntityReport> mPlayerReports;

	}
}
