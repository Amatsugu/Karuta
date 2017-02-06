var loginButton;
var windowFade;
var loginWindow;
var hasLoaded = false;
var isOpen = false;


function Open(e)
{
	if(isOpen)
		return;
	if(!hasLoaded)
	{
		$.ajax({
			url: "res/frames/login.html",
			async: true,
			success: function (text)
			{
				loginWindow.html(text);
				hasLoaded = true;
			}
		});
	}
	windowFade.fadeIn();
	isOpen = true;
}

function Close(e)
{
	if(!isOpen)
		return;
	windowFade.fadeOut();
	isOpen = false;
}

$(document).ready(function () 
{
	windowFade = $("#windowArea");
	$("#windowArea #closeZone").on("click", Close);
	loginWindow = $("#windowArea #loginWindow");
	loginButton = $("#User #menu #loginButton").on("click", Open);
});