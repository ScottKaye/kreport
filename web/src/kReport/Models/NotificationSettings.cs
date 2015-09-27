using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace kReport.Models
{
	[Serializable]
	public class NotificationSettings
    {
		/// <summary>
		/// Whether the user wishes to receive email notifications or not
		/// </summary>
		public bool ReceiveEmail { get; set; }

		/// <summary>
		/// Time of day to enable sending emails to this user
		/// </summary>
		public TimeSpan EmailStart { get; set; }

		/// <summary>
		/// Time of day to stop sending emails
		/// </summary>
		public TimeSpan EmailEnd { get; set; }
    }
}
