//#region TreeView Account Head

$(document).ready(function () {

    $('.ledger-tree ul ul').hide();

    $('.ledger-tree > ul > li > ul').show();

    // toggle click event
    $('.ledger-tree').on('click', '.parent-toggle', function () {
        var $toggle = $(this);
        var $li = $toggle.closest('li');
        var $childUl = $li.children('ul');

        if ($childUl.length === 0) return;

        if ($childUl.is(':visible')) {
            $childUl.slideUp(150);
            $toggle.removeClass('bi-dash-square-fill').addClass('bi-plus-square-fill');
        } else {
            $childUl.slideDown(150);
            $toggle.removeClass('bi-plus-square-fill').addClass('bi-dash-square-fill');

        }
    });


    $('#exportForm').on('submit', function () {
        let nodes = getExpandedNodes();
        console.log("Expand Nodes :",nodes);
        $('#expandedNodes').val(nodes.join(','));
    });

    // TreeView Report Button Click
    $('#btnTreeView').on('click', function () {
        let treeHtml = $('.ledger-tree').clone();

        treeHtml.find('i.bi').remove();

        treeHtml.find('ul').each(function () {
            let parentLi = $(this).closest('li');
            if (parentLi.find('> .parent-toggle').hasClass('bi-plus-square-fill')) {
                $(this).remove();
            }
        });

        let printWindow = window.open('about:blank', 'TreeviewReport');
        printWindow.document.write(`
            <html>
            <head>
                <title>Treeview Report</title>
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
                <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.css" rel="stylesheet">
                <style>
                    body { font-family: Arial, sans-serif; padding: 30px; }
                    .ledger-tree ul { list-style: none; padding-left: 20px; }
                    .ledger-tree li { margin: 3px 0; }
                    .tree-label { font-size: 15px; }
                    .parent-label { font-weight: bold; font-size: 17px; }
                </style>
            </head>
            <body>
                <h4 class="text-center mb-3">Chart of Accounts</h4>
                ${treeHtml.html()}
            </body>
            </html>
        `);

        printWindow.document.close();

        printWindow.onload = function () {
            printWindow.document.title = "Chart of Accounts Treeview Report";
            printWindow.focus();
            printWindow.print();
        };
    });


});

//#endregion


//#region Exand and Collapse Node Collect Function
function getExpandedNodes() {
    let expanded = [];

    $('.ledger-tree li').each(function () {
        let $li = $(this);
        let $childUl = $li.children('ul');
        if ($childUl.length > 0 && $childUl.is(':visible')) {
            let codeNo = $li.data('code');
            if (codeNo) expanded.push(codeNo);
        }
    });

    console.log("Expanded Nodes:", expanded);
    return expanded;
}

//#endregion
