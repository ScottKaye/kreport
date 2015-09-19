var app = angular.module("kApp", ["chart.js", "ui.router", "angularMoment"]);

app.config(function ($stateProvider, $urlRouterProvider) {
	$urlRouterProvider.otherwise("/Dashboard");

	$stateProvider
	.state("index", {
		url: "/Dashboard",
		templateUrl: "/Partials/index.html",
		controller: function($rootScope) {
			$rootScope.title = "Dashboard";
		}
	})
	.state("admin", {
		url: "/Admin",
		templateUrl: "/Partials/admin.html",
		controller: function ($rootScope) {
			$rootScope.title = "Admin";
		}
	});
});

app.controller("baseController", ["$http", "signalR", "$scope", "$rootScope", function ($http, signalR, $scope, $rootScope) {
	"use strict";

	var e = this;

	e.request = null;
	e.requestDetail = null;
	e.requests = [];

	e.getAllRequests = function () {
		$http.get("/k/GetAllRequests").then(function (response) {
			e.requests = response.data.map(function(c) {
				console.log(c);
				c.Date = new Date(c.Date);
				return c;
			});
		});
	};

	e.getRequest = function () {
		$http.get("k/GetRequestById", {
			params: { id: e.request.Id }
		}).then(function (response) {
			e.requestDetail = response.data;
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
}]);

//Small emitter to encapsulate global SignalR stuff
app.factory("signalR", ["$rootScope", function ($rootScope) {

	var hub = $.connection.Update;

	hub.client.test = function () {
		$rootScope.$emit("debug", "SignalR is up and operational!");
	};

	hub.client.newRequest = function (req) {
		$rootScope.$emit("newRequest", req);
	};

	$.connection.hub.start();

	return true;
}]);

//The chat controller has it's own SignalR instance, and is kept here because.
app.controller("chatController", ["$http", "$scope", function ($http, $scope) {
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
}]);

//Used for ng-repeat to reverse the order items are displayed
app.filter("reverse", function () {
	return function (items) {
		return items.slice().reverse();
	};
});