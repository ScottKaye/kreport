(function (site, undefined) {

	//TODO: Do this in Angular
	function selectRow(evt) {
		var row = $(evt.target).closest("tr");
		var id = row.attr("data-id");

		//Move selection class
		elEntries.find("tr").removeClass("selected");
		row.addClass("selected");

		ajax.GetRequestById(id, function (data) {
			var date = moment(data.Date);
			var exactDate = null;

			//If the date was over a year ago, display the year
			if (moment().diff(date, "years") > 0) {
				exactDate = date.format("MMMM Do YYYY, h:mma");
			}
			else {
				exactDate = date.format("MMMM Do, h:mma");
			}

			var $el = $("[data-placeholder=full-entry-content]");
			$el.html(
				$("<div></div>").append(
					$("<span></span>").text(exactDate)
				).append(
					$("<h3></h3>").text(data.Server ? data.Server.Name : "No server name")
				).append(
					$("<span></span")
						.addClass("sender")
						.text(data.Sender ? data.Sender.Name : "Unknown")
				).append(
					" has reported "
				).append(
					$("<span></span>")
						.addClass("target")
						.text(data.Target ? data.Target.Name : "Unknown")
				).append(
					data.Reason != null
					? $("<span></span>")
						.html(" because<br><br>")
						.append(
							$("<q></q>").text(data.Reason)
						)
					: " with no reason given."
				)
			);
			elFeTitle.html(data.Server ? data.Server.Name : "No server name");

			elJoinServer.off();
			if (data.Server.IP) {
				elJoinServer.on("click", function () {
					window.open("steam://connect/" + data.Server.IP, "_self");
				});
			}
		});
	}

}(window.site = window.site || {}));