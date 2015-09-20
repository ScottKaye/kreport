//site.js: Misc functions used around the site
//Angular stuff is in kApp.js

//Small library to interact with the querystring, unminified: https://gist.github.com/ScottKaye/e066638ba1e76642e2c9
(function (c, g) {
	function f(a) { return Object.keys(a).length ? "?" + Object.keys(a).map(function (d) { return encodeURIComponent(d) + "=" + encodeURIComponent(a[d]) }).join("&") : "" } function e() { var a = f(b); history.replaceState(null, document.title, window.location.pathname + a) } var b = {}; c.getParams = function () { var a = window.location.search.slice(1); if (0 === a.length) return []; a.split("&").forEach(function (a) { a = a.split("="); b[decodeURIComponent(a[0])] = decodeURIComponent(a[1]) }); return b }; c.removeParam = function (a) {
		delete b[a];
		e()
	}; c.removeAllParams = function () { b = {}; e() }; c.getParam = function (a) { return b[a] }; c.setParam = function (a, d) { b = c.getParams(); "object" === typeof d && (d = JSON.stringify(d)); b[a] = d; e() }; b = c.getParams()
})(window.url = window.url || {});

function toHex(c) {
	var hex = (+c).toString(16);
	return hex.length === 1 ? "0" + hex : hex;
}

function rgbToHex(r, g, b) {
	return "#" + toHex(r) + toHex(g) + toHex(b);
};

//Get exposed primary colour value from <html>
var primary = rgbToHex.apply(null, getComputedStyle(document.body.parentNode).color.match(/\d+/g));

Chart.defaults.global.colours = [primary];
Chart.defaults.global.scaleFontColor = "#ffffff";
Chart.defaults.global.responsive = false;
Chart.defaults.global.bezierCurve = false;

//Register custom elements
$(function () {
	document.registerElement("action-panel");
	document.registerElement("fancy-tooltip");
	document.registerElement("notification-toast");
});

//Renders spark/ember-style effect on the header
$(function () {
	var canvas = document.getElementById("sparks");

	//Don't bother rendering anything if the canvas has been hidden
	if (!$(canvas).is(":visible")) return;

	var w = +getComputedStyle(canvas).width.match(/\d+/g);
	var h = +getComputedStyle(canvas).height.match(/\d+/g);
	canvas.width = w;
	canvas.height = h;
	var ctx = canvas.getContext("2d");
	ctx.fillStyle = primary;

	var start = 100;

	var particles = [];
	var Particle = function () {
		this.x = Math.random() * w;
		this.y = Math.random() * start * 2 + h;
		this.radius = (Math.random() + 0.5) * 3;
		this.vx = (Math.random() - 0.5) / 20;
		this.vy = -0.3;
	};

	function animate() {
		ctx.clearRect(0, 0, w, h);

		var pIterator = particles.length;
		while (--pIterator) {
			var p = particles[pIterator];
			ctx.fillRect(p.x, p.y, p.radius, p.radius);

			p.vx += (Math.random() - 0.5) / 20;

			p.x += p.vx;
			p.y += p.vy;

			if (p.x < 0 || p.x > w || p.y < 0) {
				particles.splice(pIterator, 1);
				spawn();
			}
		}

		requestAnimationFrame(animate);
	}

	function spawn() {
		particles.push(new Particle());
	}

	for (var i = 0; i < start; ++i, spawn());

	animate();
});

//Elements with a class of "frozen" will not scroll, and will stay in their original positions
$(function () {
	$(window).on("scroll", function () {
		$(".frozen").css("transform", "translateY(" + window.scrollY + "px)");
	});
});

var notif;
var notif_timeout;

function notification(options) {
	if (!options.message) return;
	if (notif) notif.remove();

	clearTimeout(notif_timeout);

	notif = $("<notification-toast></notification-toast>").text(options.message);
	if (options.error) notif.addClass("red");
	$(document.body).append(notif);
	notif_timeout = setTimeout(function () {
		notif.remove();
	}, 3000);
}