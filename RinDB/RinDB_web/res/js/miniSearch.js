var search;
var searchDiv;
$(document).ready(function () {
    search = $("input[type=search]");
	$("#search").submit(function(e) {
		e.preventDefault();
		var query = search.val();
		window.location.href = "/1/" + encodeURIComponent(query);
	});
	
});