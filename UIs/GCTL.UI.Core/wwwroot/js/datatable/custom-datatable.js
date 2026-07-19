(function ($) {
    $.gctlGrid = {
        init: function (options) {
            var settings = $.extend({
                gridSelector: null,          
                ajaxUrl: null,               
                columns: [],                 
                checkAllSelector: null,      
                checkBoxClass: "form-check-input",   
                order: [[1, "desc"]],
                pageLength: 10,
                lengthMenu: [[10, 25, 50, -1], [10, 25, 50, 'All']],
                columnDefs: [{ targets: [0], orderable: false }],
                extraOptions: {},             
                onLoaded: function (json) { } 
            }, options);

            if (!settings.gridSelector || !$(settings.gridSelector).length) {
                return null;
            }
            if (!settings.ajaxUrl) {
                return null;
            }

            var dtOptions = $.extend(true, {
                ajax: {
                    url: settings.ajaxUrl,
                    type: "GET",
                    datatype: "json",
                    dataSrc: function (json) {
                        //console.log(json);
                       return json; // or adjust depending on your JSON structure
                    }
                },
                columnDefs: settings.columnDefs,
                columns: settings.columns,
                lengthChange: true,
                pageLength: settings.pageLength,
                lengthMenu: settings.lengthMenu,
                order: settings.order,
                language: {
                    emptyTable: "No records available"
                },
                destroy: true
            }, settings.extraOptions);

            var dataTable = $(settings.gridSelector).DataTable(dtOptions);

            $(settings.gridSelector).off('xhr.dt.gctlGrid').on('xhr.dt.gctlGrid', function (e, s, json, xhr) {
                settings.onLoaded(json);
            });

            if (settings.checkAllSelector) {
                $("body").off("click.gctlGrid", settings.checkAllSelector)
                    .on("click.gctlGrid", settings.checkAllSelector, function () {
                        $(settings.gridSelector + " ." + settings.checkBoxClass)
                            .prop('checked', $(this).prop('checked'));
                    });
            }

            return dataTable;
        },

        checkboxColumn: function (idField, checkBoxClass) {
            return {
                "data": idField,
                "className": "text-center",
                width: "2%",
                orderable: false,
                "render": function (data, type, row) {
                    return `<input type="checkbox" class="${checkBoxClass || 'checkBox'}" value="${row[idField] || ''}" />`;
                }
            };
        },

        editLinkColumn: function (idField, editSelectorClass) {
            return {
                "data": idField,
                "className": "text-center",
                width: "7%",
                "render": function (data, type, row) {
                    return `<a class='btn ${editSelectorClass}' data-id='${row[idField] || ''}'><i>${data || ''}</i></a>`;
                }
            };
        }
    };
}(jQuery));


// function loadDeductionTypes(baseUrl, gridSelector) {
//     return $.gctlGrid.init({
//         gridSelector: gridSelector,
//         ajaxUrl: baseUrl + "/grid",
//         checkAllSelector: "#deduction-type-check-all",
//         columns: [
//             $.gctlGrid.checkboxColumn("tc"),
//             $.gctlGrid.editLinkColumn("deductionTypeId", "js-deduction-type-edit"),
//             { "data": "deductionType", "autowidth": true, "className": "text-center" },
//             { "data": "shortName", "autowidth": true, "className": "text-center" }
//         ]
//     });
// }