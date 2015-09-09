$(document).ready(function () {
    $(".boxed").on("click", ".btn", function () {
        var $this = $(this);
        var table = $this.parent().children(".changes-table").children("tbody");
        var myTRs = table.children("tr");
        if (table.hasClass("collapsed")) {
            table.removeClass("collapsed");
            myTRs.show();
            $this.text("- [hide]");
        } else {
            $this.text("+ [" + (myTRs.length - 1) + " entries]");
            table.addClass("collapsed");
            myTRs.hide();
        }
    });
});