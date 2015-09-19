var app = angular.module("kApp", []);

app.controller("baseController", ["$http", "signalR", "$scope", "$rootScope", function ($http, signalR, $scope, $rootScope) {
	"use strict";

	var e = this;

	e.request = null;
	e.requestDetail = null;
	e.requests = [];

	e.getAllRequests = function () {
		$http.get("/k/GetAllRequests").then(function (response) {
			e.requests = response.data;
		});
	};

	e.getRequest = function () {
		$http.get("k/GetRequestById", {
			params: { id: e.request.Id }
		}).then(function (response) {
			e.requestDetail = response.data;
		});
	};

	e.joinSelected = function () {
		window.open("steam://connect/" + e.requestDetail.Server.IP, "_self");
	};

	$rootScope.$on("newRequest", function (evt, req) {
		$scope.$apply(function () {
			e.requests.push(req);
		});
	});

	$rootScope.$on("debug", function (evt, msg) {
		console.log(msg);
	});
}]);

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

app.controller("chatController", ["$http", "$scope", function ($http, $scope) {
	var e = this;
	var hub = $.connection.Chat;

	e.connected = false;
	e.message;
	e.messages = [];
	e.users = [];

	e.keydown = function (evt) {
		if(evt.which === 13 && e.message.length > 0) {
			hub.server.sendMessage(e.message);
			e.message = "";
		}
	};

	hub.client.sendMessage = function (msg) {
		$scope.$apply(function () {
			e.messages.push(msg);
		});

		//Scroll to bottom
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

app.filter("reverse", function () {
	return function (items) {
		return items.slice().reverse();
	};
});