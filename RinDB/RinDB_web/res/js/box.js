var box;
var curPos = 0; //Current Position
var min = 0; //Min Position
var max = 0; //Max position
var dir = 1; //Start direction
var moveSpeed = 1; //How fast to move

function Start()
{
	box = document.getElementById("box"); //Find the box
	max = window.innerWidth - box.clientWidth; //Set the max position to the width of the screen
	Move(); //Start moving
}

function Move()
{
	curPos += dir * moveSpeed; //Move the box
	if(curPos < min) //Change the direction once the edge is reached
		dir = 1;
	else if(curPos > max)
		dir = -1;
	box.style.left = curPos + "px"; //Set the new position
	setTimeout(function(){Move()}, 1); //Run this function again after 1ms
}

window.onload = Start; //Start when the page has loaded