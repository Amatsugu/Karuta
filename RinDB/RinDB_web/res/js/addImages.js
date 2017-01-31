$(document).ready(function()
{
	var end = false;
	
	
	var item = $(".inputArea");
	for(var i = 0; i < 20; i ++)
	{
		item.clone().appendTo("#advancedSearch").children("label").text("Item " + i);
	}
	
});
