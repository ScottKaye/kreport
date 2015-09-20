var app = angular.module("kApp", ["chart.js", "ui.router", "angularMoment"]);

app.config(function ($compileProvider, $httpProvider, $stateProvider, $urlRouterProvider) {
	//Allow steam: protocol URLs
	$compileProvider.aHrefSanitizationWhitelist(/^\s*(https?|steam):/);

	//Send POST through URL instead of body
	$httpProvider.defaults.transformRequest = function (data) {
		if (data === undefined) {
			return data;
		}
		return $.param(data);
	}
	$httpProvider.defaults.headers.post["Content-Type"] = "application/x-www-form-urlencoded; charset=UTF-8";

	$urlRouterProvider.otherwise("/Dashboard");
	$stateProvider
	.state("index", {
		url: "/Dashboard",
		templateUrl: "/Partials/index.html",
		controller: function ($rootScope) {
			$rootScope.title = "Dashboard";
		}
	})
	.state("admin", {
		url: "/Admin",
		templateUrl: "/Partials/admin.html",
		controller: function ($rootScope) {
			$rootScope.title = "Admin";
		}
	})
	.state("start", {
		url: "/Start",
		templateUrl: "/Partials/start.html",
		controller: function ($rootScope) {
			$rootScope.title = "First Run";
		}
	});
});

app.controller("baseController", function ($http, signalR, $scope, $rootScope) {
	"use strict";

	var e = this;

	e.request = null;
	e.requestDetail = null;
	e.requests = [];
	e.user = {};
	e.thisWeek;
	e.thisYear;
	e.serverStats;
	e.serverLabels;

	e.getAllRequests = function () {
		$http.get("/k/GetAllRequests").then(function (response) {
			e.requests = response.data.map(function (c) {
				c.Date = new Date(c.Date);
				c.DateString = moment(c.Date).format("LLL");
				return c;
			});
		});
	};

	e.login = function () {
		console.log(e.user);

		$http.post("/k/Login", e.user).then(function (response) {
			window.location = window.location.origin;
		}, function (response) {
			notification({
				message: response.data,
				error: true
			});
		});
	};

	$http.get("/k/GetNumRequestsThisWeek").then(function (response) {
		e.thisWeek = [response.data];
	});

	$http.get("/k/GetNumRequestsThisYear").then(function (response) {
		e.thisYear = [response.data];
	});

	$http.get("/k/GetServerStats").then(function (response) {
		e.serverLabels = Object.keys(response.data);
		e.serverStats = [e.serverLabels.map(function (c) {
			return response.data[c];
		})];
	});

	function getChecked() {
		return e.requests.filter(function (c) {
			return c.checked;
		}).map(function (c) {
			return c.Id;
		});
	}

	e.uncheckAll = function () {
		e.requests.map(function (c) {
			c.checked = false;
		});
	};

	e.checkedDone = function (done) {
		//done will be true to mark as done, false to unmark
		$http.post("k/Done", {
			ids: getChecked(),
			done: done
		}).then(function (response) {
			notification({
				message: done ? "Marked as done." : "Unmarked as done."
			});
			e.uncheckAll();
		}, function (response) {
			notification({
				message: "Failed to change mark.",
				error: true
			});
		});

		e.requests.filter(function (c) {
			return c.checked;
		}).map(function (c) {
			c.Done = done;
		});
	};

	e.checkedDelete = function () {
		var ids = getChecked();
		$http.post("k/Delete", {
			ids: ids
		}).then(function (response) {
			notification({
				message: "Request" + (ids.length != 1 ? "s" : "") + " deleted."
			});
			e.uncheckAll();
		}, function (response) {
			notification({
				message: "Failed to delete items",
				error: true
			});
		});

		e.requests = e.requests.filter(function (c) {
			return !c.checked;
		});
	};

	//Launches the steam protocol handler to join a server
	e.joinSelected = function () {
		window.open("steam://connect/" + e.requestDetail.Server.IP, "_self");
	};

	//Fires when a new request is received from SignalR
	$rootScope.$on("newRequest", function (evt, req) {
		$scope.$apply(function () {
			e.requests.push(req);
		});
	});

	//Fires when TestHub is called from SignalR
	$rootScope.$on("debug", function (evt, msg) {
		console.log(msg);
	});
});

//Small emitter to encapsulate global SignalR stuff
app.factory("signalR", function ($rootScope) {
	"use strict";

	var hub = $.connection.Update;

	hub.client.test = function () {
		$rootScope.$emit("debug", "SignalR is up and operational!");
	};

	hub.client.newRequest = function (req) {
		$rootScope.$emit("newRequest", req);
	};

	$.connection.hub.start();

	return true;
});

//The chat controller has it's own SignalR instance, and is kept here because.
app.controller("chatController", function ($http, $scope) {
	"use strict";

	var e = this;
	var hub = $.connection.Chat;

	e.connected = false;
	e.message;
	e.messages = [];
	e.users = [];

	e.keydown = function (evt) {
		if (evt.which === 13 && e.message.length > 0) {
			hub.server.sendMessage(e.message);
			e.message = "";
		}
	};

	hub.client.sendMessage = function (msg) {
		$scope.$apply(function () {
			e.messages.push(msg);
		});

		//Scroll chat to bottom on new message
		$(".chat-messages").each(function () {
			this.scrollTop = this.scrollHeight;
		});
	};

	hub.client.updateUsers = function (users) {
		$scope.$apply(function () {
			e.users = users;
		});
	};

	$.connection.hub.start().done(function () {
		$scope.$apply(function () {
			e.messages.push({
				system: true,
				author: {},
				message: "Connected."
			});
			e.connected = true;
		});
	});
});

app.controller("startController", function ($http, $state) {
	"use strict";

	var e = this;

	e.Email;
	e.Password;
	e.ConfirmPassword;

	e.create = function () {
		$http.post("/k/FirstUser", {
			Email: e.Email,
			Password: e.Password,
			ConfirmPassword: e.ConfirmPassword
		}).then(function (response) {
			window.location = window.location.origin;
		}, function (response) {
			notification({
				message: response.data,
				error: true
			});
		});
	};
});

app.controller("adminController", function ($http) {
	"use strict";

	var e = this;

	e.settings = {};

	e.saveSettings = function () {
		$http.post("/k/SaveSettings", {
			settings: JSON.stringify(e.settings)
		}).then(function (response) {
			window.location.reload();
		});
	};

	//Load settings asynchronously to let the server handle security
	$http.get("/k/GetSettings").then(function (response) {
		e.settings = response.data;
	});
});

//Used for ng-repeat to reverse the order items are displayed
app.filter("reverse", function () {
	return function (items) {
		return items.slice().reverse();
	};
});