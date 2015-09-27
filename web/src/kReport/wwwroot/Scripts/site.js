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

//Small notification library
(function (notify, undefined) {
	var defaults = {
		message: null,
		error: false,
		refresh: false,
		options: [],
		timeout: 3000
	},
    container;

	function extendOptions(o1, o2) {
		var n = {
			message: o2.message,
			error: o2.error,
			refresh: o2.refresh,
			options: o2.options,
			timeout: o2.timeout
		};
		for (var prop in o2)
			if (o1.hasOwnProperty(prop)) n[prop] = o1[prop];
		return n;
	}

	function Notif(options) {
		var self = this;

		var el = document.createElement("div");
		el.classList.add("notification");
		if (options.error) el.classList.add("error");
		el.innerText = options.message;

		var progress = document.createElement("div");
		progress.classList.add("progress");
		el.appendChild(progress);
		progress.style.animationDuration = options.timeout + "ms";

		if (options.options) {
			var buttons = document.createElement("div");
			buttons.classList.add("options");
			options.options.forEach(function (c) {
				var btn = document.createElement("button");
				btn.innerText = c.title;
				btn.onclick = c.callback.bind(self);
				buttons.appendChild(btn);
			});
			el.appendChild(buttons);
		}

		this.options = options;
		this.hidden = true;
		this.element = el;
	}

	Notif.prototype.hide = function () {
		container.removeChild(this.element);
		this.hidden = true;
		return this;
	};

	Notif.prototype.show = function () {
		if (this.options.refresh) {
			var to = this.options.refresh;
			delete this.options.refresh;
			delete this.options.options;
			localStorage["nnp"] = JSON.stringify(this.options);
			if (to === true) {
				window.location.reload();
			} else {
				window.location = window.location.origin + window.location.pathname + to;
				window.location.reload();
			}
			return;
		}

		this.element.classList.add("in");
		window.setTimeout(function () {
			this.element.classList.remove("in");
		}.bind(this), 250);
		container.appendChild(this.element);

		if (this.options.timeout > 0) {
			window.setTimeout(function () {
				if (!this.hidden) {
					this.element.classList.add("out");
					window.setTimeout(function () {
						container.removeChild(this.element);
					}.bind(this), 500);
					this.hidden = true;
				}
			}.bind(this), this.options.timeout);
		}

		this.hidden = false;
		return this;
	};

	notify.create = function (options) {
		return new Notif(extendOptions(options || {}, defaults));
	};

	notify.show = function (options) {
		return notify.create(options).show();
	};

	(function () {
		container = document.createElement("section");
		container.className = "notification-container";
		document.body.appendChild(container);

		var nnp = localStorage.nnp;
		if (nnp) try {
			notify.show(JSON.parse(nnp));
			delete localStorage.nnp;
		} catch (e) { }
	})();
}(window.notify = window.notify || {}));