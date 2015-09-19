//Forces all post forms (except those with a "data-noajax" attribute) to submit through AJAX.
//Also provides error handling for responses from the server.

/*

<form method="post">
	<script class="form-scripts">
		[{
			success: function () {
				alert("Success");
			},
			error: function () {
				alert("Error");
			}
		}];
	</script>
	... Form things ...
</form>

This form will call the relative alert if the ajax call succeeds or fails.

 */

$(function () {
	$forms = $("form:not([data-noajax])[method=post]");

	$forms.on("submit", function (e) {
		e.preventDefault();

		var $form = $(this);

		var scripts = null;
		var $scripts = $form.find("script.form-scripts");
		if ($scripts.length) {
			scripts = new Function("return " + $scripts[0].textContent.trim())();
			scripts = scripts[0];
		}

		$.ajax({
			method: this.method,
			url: this.action,
			data: $form.serialize(),
			success: function (data) {
				$form.find(".form-error").text("");
				$form.find(".form-success").text(data);
				if (scripts.success) scripts.success();
			},
			error: function (data) {
				console.log(data);
				var message = data.hasOwnProperty("responseText") ? data.responseText : data.status + " " + data.statusText;

				$form.find(".form-success").text("");
				$form.find(".form-error").text(message);
				if (scripts.error) scripts.error();
			}
		});
	});
});