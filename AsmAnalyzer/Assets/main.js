$(document).ready(function () {
    $(".boxed").on("click", ".btn", function() {
        triggertable(this, $(this).parent().children(".changes-table").children("tbody"), false);
    });
    $(".meta-box").on("click", ".btn", function() {
        triggertable(this, $(this).parent().children(".changes-table").children("tbody"), false);
    });

    $(".meta-box").on("click", ".btn-success", function () {
        triggertable(this, $(".asm-success"), true);
    });
    $(".meta-box").on("click", ".btn-errors", function () {
        triggertable(this, $(".asm-errors"), true);
    });


    $(".meta-box").ready(function () {
        var e = $(".meta-box").children(".btn").first();
        //the 2 following does not trigger
        triggertable(e, $(".asm-success"), true);
        triggertable(e, $(".asm-errors"), true);
        triggertable(e, e.parent().children(".changes-table").children("tbody"), false);
    });
});

function triggertable(e, table, isTr) {
    var $this = $(e);
    var myTRs = isTr ? table : table.children("tr");
    if (table.hasClass("collapsed")) {
        table.removeClass("collapsed");
        myTRs.show();
        $this.text("- [hide]");
    } else {
        var count;
        if (isTr) {
            var asmTables = table.find(".asm-table").children("tbody").children("tr");
            count = asmTables.length / 2;
        } else {
            count = myTRs.length > 0 ? myTRs.length - 1 : 0;
        }
        $this.text("+ [" + count + " entries]");
        table.addClass("collapsed");
        myTRs.hide();
    }
};