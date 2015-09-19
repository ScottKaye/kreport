var app = angular.module("kAppM", []);

/*app.controller("baseController", ["$http", "signalR", "$scope", "$rootScope", function ($http, signalR, $scope, $rootScope) {
	"use strict";

	var e = this;

	e.request = null;
	e.requestDetail = null;
	e.requests = [];

	$http.get("k/GetAllRequests").then(function (response) {
		e.requests = response.data;
	});

	e.getRequest = function () {
		$http.get("k/GetRequestById", {
			params: {
				id: e.request.Id
			}
		}).then(function (response) {
			e.requestDetail = response.data;
			console.log(e.requestDetail);
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
}]);*/

app.controller("baseController", function ($http) {
	var e = this;

	e.server = localStorage.server;

	e.showSettings = function () {
		navigator.notification.vibrate(1000);
	};
});

app.controller("connectController", function ($http) {
	var e = this;

	e.connecting = false;
	e.button = "Connect";
	e.serverAddress;
	e.result;
	e.message;

	function success() {
		e.connecting = false;
		e.result = "success";
		e.button = "Success";
		e.message = "This server has saved.  You will automatically connect to it from now on.";
		localStorage.server = e.serverAddress;
	}

	function fail() {
		e.connecting = false;
		e.result = "fail";
		e.button = "Failure";
		e.message = "kReport doesn't appear to be running on " + e.serverAddress + ".";
	}

	e.connect = function () {
		if (!e.serverAddress) return;

		e.connecting = true;
		e.result = null;
		e.button = "Connecting...";

		e.serverAddress = e.serverAddress.toLowerCase();
		if (e.serverAddress.slice(-1) != "/") e.serverAddress += "/"; //Ends with /
		if (e.serverAddress.substring(0, 7) != "http://") e.serverAddress = "http://" + e.serverAddress; //Starts with http (at least)

		e.serverAddress = e.serverAddress;

		$http.get(e.serverAddress + "k/IsApp").then(function (response) {
			if (response.status === 202) success();
			else fail();
		}, fail);
	};
});

/*
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
*/
