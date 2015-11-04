$(document).ready(function () {
    $(".boxed").on("click", ".btn", function() {
        triggertable(this, $(this).parent().children(".changes-table").children("tbody"), false);
    });
    $(".meta-box").on("click", ".btn", function() {
        var e = $(this).parent().children(".changes-table").children("tbody");
        triggertable(this, e, false);
        var btnSuccess = $(".btn-success").first();
        var btnErrors = $(".btn-errors").first();
        collapse(btnSuccess, $(".asm-success"));
        collapse(btnErrors, $(".asm-errors"));
    });

    $(".meta-box").on("click", ".btn-success", function () {
        triggertable(this, $(".asm-success"), true);
    });
    $(".meta-box").on("click", ".btn-errors", function () {
        triggertable(this, $(".asm-errors"), true);
    });


    $(".meta-box").ready(function () {
        var e = $(".meta-box").children(".btn").first();
        var btnSuccess = $(".btn-success").first();
        var btnErrors = $(".btn-errors").first();
        collapse(btnSuccess, $(".asm-success"));
        collapse(btnErrors, $(".asm-errors"));
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
            count = Math.round(asmTables.length / 2);
        } else {
            count = myTRs.length > 0 ? myTRs.length - 1 : 0;
        }
        $this.text("+ [" + count + " entries]");
        table.addClass("collapsed");
        myTRs.hide();
    }
};

function collapse(btn,table) {
    var count;
    var asmTables = table.find(".asm-table").children("tbody").children("tr");
    count = Math.round(asmTables.length / 2);
    btn.text("+ [" + count + " entries]");
    table.addClass("collapsed");
    table.hide();
};