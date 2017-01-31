var tagD;
$(document).ready(function()
{
	tagD = $("#tagDescription");
	tagD.css("opacity", 0);
	$(".tag").hover(function(e)
	{
		tagD.animate(
		{
			opacity: 1,
			left: e.currentTarget.offsetLeft,
			top: e.currentTarget.offsetTop - tagD.height() - 10,
		},
		{
			duration: 500,
			queue: false
		});
		tagD.html("loading...");
		$.ajax(
			{
				url:"/DB/tags/description/" + e.currentTarget.textContent,
				async: true,
				success: function(text)
				{
					if(text == "")
						text = "Error";
					tagD.html(text);
				}
			});
		//tagD.fadeIn();
		//tagD.css("left:" + e.pageX + "px; top:" + e.currentTarget.offsetTop + "px");
	}, out);
});


function out()
{
	tagD.animate(
		{
			opacity: 0
		}, { duration: 500, queue:false});
	//tagD.fadeOut();
}