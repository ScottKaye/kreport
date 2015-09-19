using Newtonsoft.Json;

namespace kReport.Models
{
	public class ChatMessage
	{
		[JsonProperty("author")]
		public ChatUser author;

		[JsonProperty("message")]
		public string message = "";

		[JsonProperty("date")]
		public string date;

		[JsonProperty("system")]
		public bool system = false;
	}
}