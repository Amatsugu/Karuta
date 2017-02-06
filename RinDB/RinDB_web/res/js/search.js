var search;
var searchField;
var content;
var curReq = "/DB/latest/";
var count = 6 * 3;
var page = 1;
var lastReq;
var useInit = false;
var isEnd = false;
var lastQuery;

$(document).ready(function () {
    searchDiv = $("#search");
    content = $("#contentGrid");
    search = $("input[type=search]");
    page = search.data("page");
    if (page == undefined)
        page = 1;
    content.html("<div class=\"searching\">Loading...</div>");
    lastReq = $.ajax(
	{
	    url: curReq + count + "/" + page,
	    async: true,
	    success: function (text) {
	        content.html(text);
	        $(".imageCard").fadeIn();
	    }
	});
    $(window).scroll(function () {
        if (isEnd)
            return;
        if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight) {
            //console.log(page);
            page++;
            lastReq = $.ajax(
			{
			    url: curReq + count + "/" + page,
			    async: true,
			    success: function (text) {
			        if (text == "" || text == null || text == undefined) {
			            isEnd = true;
			            page--;
			            return;
			        }
			        content.append(text);
			        $(".imageCard").fadeIn();
			        if (lastQuery == "" || lastQuery == undefined || lastQuery == null)
			            history.pushState("RinDB", "Home", "/" + page);
			        else
			            history.pushState("RinDB \"" + lastQuery + "\"", "Search", "/" + page + "/" + encodeURIComponent(lastQuery))
			    },
				fail: function()
				{
					content.html("<div class=\"searching\">An Error Occured</div>");
				}
			});
        }
    });
    //Seach Feild
    searchDiv.submit(function (e) {
        e.preventDefault();
        history.pushState("RinDB \"" + search.val() + "\"", "Search", "/" + page + "/" + encodeURIComponent(search.val()));
    });

    search.on("propertychange change keyup input paste submit", function (e) {
        e.preventDefault();
        var query = search.val();
        if (lastQuery != query) {
            lastQuery = query;
            isEnd = false;
            if (e.type == "submit")
                history.pushState("RinDB \"" + lastQuery + "\"", "Search", "/" + page + "/" + encodeURIComponent(lastQuery));
            if (!useInit) {
                page = 1;
            } else
                useInit = false;
            if (lastReq && lastReq.readyState != 4)
                lastReq.abort();
            if (query == "") {
                curReq = "/DB/latest/";
            } else {
                curReq = "/DB/search/" + query + "/";
            }
            content.html("<div class=\"searching\">Searching...</div");
            var curPage;
            lastReq = $.ajax(
			{
			    url: curReq + count + "/" + page,
			    async: true,
			    success: function (text) {
			        if (text == "")
			            text = "<div class=\"searching\">No Results found</div";
			        content.html(text);
			        $(".imageCard").fadeIn();
			    },
				fail: function()
				{
					content.html("<div class=\"searching\">An Error Occured</div>");
				}
			});
        }
    });
    if (search.val() != "") {
        useInit = true;
        search.val(decodeURIComponent(search.val()));
        search.trigger("input");
    }
    //Advanced Search
    var arrow = $("#advancedSearchButton");
    arrow.on("click", function () {
        if (searchDiv.css("height") == "45px") {
            searchDiv.css("height", "450px");
            arrow.css("transform", "rotate(-90deg)");
        }
        else {
            searchDiv.css("height", "45px");
            arrow.css("transform", "rotate(0deg)");
        }
    });
});



function GetMatchingTags(query) {
    $.ajax(
	{
	    url: curReq + count + "/" + page,
	    async: false,
	    success: function (text) {
	        if (text == "")
	            return;
	        content.html(text);
	        $(".imageCard").fadeIn();
	    }
	});
    var outTags;
    for (var i = 0; i < tags.length; i++) {
        if (tags[i].search(query.toLocaleLowerCase()))
            outTags += tags[i] + "|";
    }
    return outTags.split("|");
}
