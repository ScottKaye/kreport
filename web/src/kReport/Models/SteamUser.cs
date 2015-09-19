using kReport.Infrastructure;

namespace kReport.Models
{
	public class SteamUser
	{
		private string _Name;

		//Get these values with https://sm.alliedmods.net/new-api/clients/GetClientAuthId
		public string ID3 { get; set; }
		public string CommunityID { get; set; }
		public string Name
		{
			get { return _Name; }
			set
			{
				_Name = Helpers.Sanitize(value, 25);
			}
		}
	}
}
