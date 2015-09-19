(function (ajax, undefined) {

	function generalError(data) {
		console.error("AJAX error occured:");
		console.error("Data:", data);
		console.error("Context:", arguments);
	}

	ajax.GetAllRequests = function (callback) {
		$.ajax({
			url: "k/GetAllRequests",
			method: "get",
			success: callback,
			error: generalError
		});
	};

	ajax.GetRequestById = function (id, callback) {
		console.log("Sending ", id);
		$.ajax({
			url: "k/GetRequestById",
			method: "get",
			data: { id: id },
			success: callback,
			error: generalError
		});
	};

}(window.ajax = window.ajax || {}));