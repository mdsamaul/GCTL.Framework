        let selectedCurrencies = [];
        let currentPage = 1;
        let totalPages = 1;
        let pageSize = 10;
        let currentSortColumn = 'CurrencyId';
        let currentSortDirection = 'desc';
        console.log("currency page")
        console.log(`Global Call: ${selectedCurrencies}`);         
        function showDecimalFormat(decimalPlaceValue) {
            let currencySymbol = $('#currencySymbol').val() || "";
            decimalPlaceValue = Math.min(decimalPlaceValue, 3);

            let number = 1.123456;
            let formattedNumber = number.toFixed(decimalPlaceValue);

            $('#showFormat').val(formattedNumber + currencySymbol);
            $('#negativeFormat').val("(" + formattedNumber + ")" + currencySymbol);
        }

        // Attach input event
        $(document).on('input', '#decimalPlaces, #currencySymbol', function () {
            let decimalValue = parseInt($('#decimalPlaces').val());

            if (isNaN(decimalValue)) {
                $('#showFormat').val("");
                $('#negativeFormat').val("");
                return;
            }

            showDecimalFormat(decimalValue);
        });

         // Getting CurrencyID
        function loadNextCurrencyId(){
            $.ajax({
                url: '/next-currency-id', 
                method: 'GET',
                success: function (response) {
                    $('#currencyID').val(response);
                    console.log("Next CurrencyID Called");
                },
                error: function (xhr, status, error) {
                    console.error("Failed to load next currency ID:", error);
                }
            });
        }

         // Select all checkbox
        $(document).on('click','#selectAll',function () {
                const isChecked = $(this).prop('checked');
                $('.row-checkbox:visible').prop('checked', isChecked);

                if (isChecked) {
                    $('.row-checkbox:visible').each(function () {
                        const id = parseFloat($(this).data('tcidcheck'));
                        if (!selectedCurrencies.includes(id)) {
                            selectedCurrencies.push(id);
                        }
                    });
                } else {
                    $('.row-checkbox:visible').each(function () {
                        const id = parseFloat($(this).data('tcidcheck'));
                        const index = selectedCurrencies.indexOf(id);
                        if (index !== -1) {
                            selectedCurrencies.splice(index, 1);
                        }
                    });
                }
              console.log("Selected Currencies:", selectedCurrencies);
        });

        // Save and Update Currencies with checking Duplicate 
        function saveCurrency() {
            console.log("Save currency called");
            let currencyName =$("#currencyName").val().trim();
            if(!currencyName || currencyName == ""){
                toastr.warning("Please enter currency name");
                $('#currencyName').addClass('error-border').focus();
                return false;
            }

            let tc = $('#tcID').val();
            let url = '/currency';
            let method = 'POST';
            let successMessage = 'Data Saved Successfully';

            if (tc) {
                url = `/currency/${tc}`;
                method = 'PUT';
                successMessage = 'Data Updated Successfully';
            }

            let currencyData = {
                TC: parseInt($("#tcID").val()) || 0,
                CurrencyId: $("#currencyID").val(),
                CurrencyName: $("#currencyName").val(),
                DecimalPlaces: parseInt($("#decimalPlaces").val()) || 0,
                NegativeFormat: $("#negativeFormat").val(),
                Symbol: $("#currencySymbol").val(),
                ShortName: $("#currencyShortName").val()
            };

            // Check for duplicate first
            $.ajax({
                url: '/check-duplicate',
                type: 'POST',
                data: currencyData, 
                success: function (response) {
                    if (response.isDuplicate) {
                        toastr.warning(response.message || "Data Already Exists!");
                        return;
                    }

                    // If not duplicate data getting, then proceed to save or update
                    $.ajax({
                        url: url,
                        type: method,
                        contentType: 'application/json',
                        data: JSON.stringify(currencyData),
                        success: function (response) {
                            if (response.isSuccess) {
                                toastr.success(successMessage);
                                selectedCurrencies = [];
                                clearForm();
                                loadCurrencies(1, 10);
                            } else {
                                toastr.error(response.message || "Insertion Failed!");
                                console.error('Save error:', response.message);
                            }
                        },
                        error: function (xhr, status, error) {
                            const msg = xhr.responseJSON?.message || "An error occurred while saving.";
                            toastr.error(msg);
                            console.error('Error:', msg);
                        }
                    });
                },
                error: function (xhr, status, error) {
                    toastr.error("Error checking duplicate.");
                    console.error("Duplicate check error:", xhr.responseText || error);
                }
            });
        }

        function clearForm() {
                $("#tcID").val("");
                $("#currencyID").val("");
                $("#currencyName").val("");
                $("#decimalPlaces").val("");
                $("#negativeFormat").val("");
                $("#currencySymbol").val("");
                $("#showFormat").val("");
                $('#searchInput').val('');
                $("#currencyShortName").val("");
                $(".form-control").removeClass("is-invalid is-valid");
                $(".invalid-feedback, .valid-feedback").remove();
                $('#modifyDateContainer').hide();
                $('#lDateContainer').hide();
            selectedCurrencies = [];
            $('.row-checkbox').prop('checked', false);
            $('#selectAll').prop('checked', false);
                console.log("I am in currency");
                loadNextCurrencyId(); // Reload next ID for new entry
                currentPage = 1;
                const emptySearch = '';
                loadCurrencies(1, pageSize, currentSortColumn, currentSortDirection, emptySearch);
        }

        //Getting All data with pagination
        function loadCurrencies(page = 1, pageSize = 10, sortColumn = currentSortColumn, sortDirection = currentSortDirection, searchTerm = "") {
            currentPage = page;

            $.ajax({
                url: '/currency-list',
                type: 'GET',
                data: {
                    pageNumber: page,
                    pageSize: pageSize,
                    searchTerm: searchTerm,
                    sortColumn: sortColumn,
                    sortOrder: sortDirection
                },
                success: function (response) {
                    console.log('currency Table', response);
                    let rows = '';

                    if (response.data && response.data.length > 0) {
                        response.data.forEach(function (item) {
                            rows += `<tr>
                                <td class="text-center">
                                    <input type="checkbox" class="form-check-input form-check-input-sm row-checkbox"
                                           style="width: 12px; height: 12px;" data-tcidcheck="${item.tc}">
                                </td>
                                <td class="text-center">
                                    <button class="btn btn-transfer"
                                        data-tcidbtn="${item.tc}"
                                        data-currencyid="${item.currencyId}"
                                        data-currencyname="${item.currencyName}"
                                        data-shortname="${item.shortName}"
                                        data-decimalplaces="${item.decimalPlaces}"
                                        data-negativeformat="${item.negativeFormat}"
                                        data-symbol="${item.symbol}"
                                        data-ldate="${item.lDate}"
                                        data-modifydate="${item.modifyDate}">
                                        ${item.currencyId}
                                    </button>
                                </td>
                                <td class="text-start align-middle">${item.currencyName || ''}</td>
                                <td class="text-center align-middle">${item.shortName || ''}</td>
                                <td class="text-center align-middle">${item.symbol || ''}</td>
                                <td class="text-center align-middle">${item.decimalPlaces || ''}</td>
                                <td class="text-center align-middle">${item.negativeFormat || ''}</td>
                            </tr>`;
                        });

                        $('#tblBody').html(rows);

                        //  paginationInfo 
                        const totalRecords = response.totalCount || 0;
                        const pagination = response.paginationInfo || {};
                        totalPages = pagination.totalPages || 1;

                        const startIndex = pagination.startItem || 0;
                        const endIndex = pagination.endItem || 0;

                        $('#paginationSummary').text(`Showing ${startIndex} to ${endIndex} of ${totalRecords} entries`);
                        $('#paginationInfo').text(`Page ${currentPage} of ${totalPages}`);
                        $('#totalRecordsInfo').text(`Total Records: ${totalRecords}`);

                        generatePageButtons(currentPage, totalPages);
                        //$('#selectAll').prop('checked', false);
                        updateCheckboxState();
                        updateSortIndicators();
                    } else {
                        $('#tblBody').html(`<tr class="no-data"><td colspan="7" class="text-center">No Data Available</td></tr>`);
                        $('#paginationSummary').text('');
                        $('#paginationInfo').text('');
                        $('#totalRecordsInfo').text('');
                        $('#pageNavigation').html('');
                        $('#selectAll').prop('checked', false);
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Error loading data: ", error);
                    $('#tblBody').html(`<tr class="no-data"><td colspan="7" class="text-center">Failed to load data.</td></tr>`);
                    $('#paginationSummary').text('');
                    $('#paginationInfo').text('');
                    $('#totalRecordsInfo').text('');
                    $('#pageNavigation').html('');
                    $('#selectAll').prop('checked', false);
                }
            });
        }

        //Edit Details loading data
        $(document).on('click', '.btn-transfer', function () {
            const data = {
                tc: $(this).data('tcidbtn'),  // also fix this from 'tcidcheck' to 'tcidbtn'
                currencyId: $(this).data('currencyid'),
                currencyName: $(this).data('currencyname'),
                shortName: $(this).data('shortname'),
                symbol: $(this).data('symbol'),
                decimalPlaces: $(this).data('decimalplaces'),       
                negativeFormat: $(this).data('negativeformat'), 
                lDate: $(this).data('ldate'),
                modifyDate: $(this).data('modifydate')
            };
            console.log("Populated Data:", data);
            populateCurrencyForm(data);
            //showDecimalFormat(parseInt(data.decimalPlaces || 0));
            selectedCurrencies =[];
            populateCurrencyIDs(data.tc); // assuming this is your separate function
        });
        //Collected TC IDs
        function populateCurrencyIDs(IDs){
                let IDvalue = parseFloat(IDs);
                selectedCurrencies.push(IDvalue);
                console.log(`IDvalue: ${IDvalue}`);
                console.log(`Call From ${selectedCurrencies}`);
        }
        //Showing data in UI 
        function populateCurrencyForm(data) {
            console.log("From Currency Form Population Function, data are:", data);
            $('#currencyForm input[name="tcID"]').val(data.tc);
            $('#currencyForm input[name="currencyID"]').val(data.currencyId); 
            $('#currencyForm input[name="currencyName"]').val(data.currencyName);
            $('#currencyForm input[name="currencyShortName"]').val(data.shortName);
            $('#currencyForm input[name="currencySymbol"]').val(data.symbol);
            let decimalValue = (data.decimalPlaces && parseFloat(data.decimalPlaces) !== 0) ? data.decimalPlaces : '';
            $('#currencyForm input[name="decimalPlaces"]').val(decimalValue);
            //$('#currencyForm input[name="decimalPlaces"]').val(data.decimalPlaces);
            $('#currencyForm input[name="negativeFormat"]').val(data.negativeFormat);

            if (data.lDate) {
                const formattedLDate = new Date(data.lDate).toLocaleDateString('en-GB');
                $('#displayLDate').text(formattedLDate);
                $('#lDateContainer').show(); 
            }
            else {
                $('#lDateContainer').hide(); 
            }

            // Handle Modify Date
            if (data.modifyDate) {
                const formattedModifyDate = new Date(data.modifyDate).toLocaleDateString('en-GB');
                $('#displayModifyDate').text(formattedModifyDate);
                $('#modifyDateContainer').show(); 
            }
            else {
                $('#modifyDateContainer').hide(); 
            }
        }

        //deleting Currencies
        function deleteCurrencyList() {
            let idsToDelete = [...selectedCurrencies];

            if (idsToDelete.length === 0) {
                toastr.warning("Please select at least one item to delete.");
                return;
            }

            let ids = JSON.stringify(idsToDelete);
            console.log(`IDs to be deleted: ${ids}`);

            $.ajax({
                url: '/currency-list-bulkdelete',
                type: 'DELETE',
                data: ids,
                contentType: 'application/json',
                success: function (response) {
                    clearForm();
                    loadCurrencies(currentPage, pageSize, currentSortColumn, currentSortDirection);
                    selectedCurrencies = [];

                    if (response.isSuccess) {
                        toastr.success("Data deleted successfully!");
                    } else {
                        toastr.error(response.message || "Delete failed!");
                        console.error('Delete error:', response.message);
                    }
                },
            error: function (xhr) {
                let errorMessage = "An error occurred while deleting currency!";
                try {
                    const res = JSON.parse(xhr.responseText);
                    if (res.message) {
                        errorMessage = res.message;
                    }
            } 
            catch (e) {
                // JSON parse failed, ignore
            }
            toastr.error(errorMessage);
            console.error('Error deleting currencies:', xhr);
            }

            });
        }

        // Function to update sort indicators in the table header
        function updateSortIndicators() {
            $('.sortable').removeClass('sort-asc sort-desc');
            $('.sortable .sort-icon').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

            const activeHeader = $(`.sortable[data-column="${currentSortColumn}"]`);
            activeHeader.addClass(`sort-${currentSortDirection}`);

            if (currentSortDirection === 'asc') {
                activeHeader.find('.sort-icon').removeClass('fa-sort fa-sort-down').addClass('fa-sort-up');
            } else {
                activeHeader.find('.sort-icon').removeClass('fa-sort fa-sort-up').addClass('fa-sort-down');
            }
        }

        $(document).on('click', '.sortable', function () {
            const column = $(this).data('column');

            if (currentSortColumn === column) {
                // Toggle direction
                currentSortDirection = (currentSortDirection === 'asc') ? 'desc' : 'asc';
            } else {
                // New column clicked
                currentSortColumn = column;
                currentSortDirection = 'desc'; 
            }

            loadCurrencies(1, pageSize, currentSortColumn, currentSortDirection, $('#searchInput').val());
        });

        function generatePageButtons(currentPage, totalPages) {
            const navigationDiv = $('#pageNavigation');
            navigationDiv.empty();

            const createButton = (page, text = null) => {
                const button = $('<button>')
                    .addClass('btn btn-secondary mx-1 btn-sm')
                    .text(text || page)
                    .click(function () {
                    loadCurrencies(page, pageSize, currentSortColumn, currentSortDirection);
                });
                if (page === currentPage) {
                    button.removeClass('btn-secondary').addClass('btn-primary');
                }
                return button;
            };

            // Show first page
            navigationDiv.append(createButton(1));

            // Add ellipsis before current page if needed
            if (currentPage > 2) {
                 navigationDiv.append($('<span>').text('...').addClass('mx-1'));
            }

            // Show current page if it is not first or last
            if (currentPage !== 1 && currentPage !== totalPages) {
                navigationDiv.append(createButton(currentPage));
            }

            // Add ellipsis after current page if needed
            if (currentPage < totalPages - 1) {
                navigationDiv.append($('<span>').text('...').addClass('mx-1'));
            }

            // Show last page
             if (totalPages > 1) {
                navigationDiv.append(createButton(totalPages));
             }
        }

         // Function to update checkbox state based on selectedExpenseTypes
        function updateCheckboxState() {
             $('.row-checkbox').each(function() {
                 const currencyID = $(this).data('tcidcheck');
                 $(this).prop('checked', selectedCurrencies.includes(currencyID));
             });

             // Check if all currently visible checkboxes are selected
             const allChecked = $('.row-checkbox:visible').length > 0 &&
                              $('.row-checkbox:visible:not(:checked)').length === 0;
             $('#selectAll').prop('checked', allChecked);
         }

         // Debounce function to delay the execution of a given function
        function debounce(func, delay) {
                let timer;
                return function () {
                    const context = this, args = arguments;
                    clearTimeout(timer);
                    timer = setTimeout(() => func.apply(context, args), delay);
                };
            }

        // Individual checkbox click
        function bindCheckboxHandlers() {
              $(document).off('click', '.row-checkbox'); // Unbind previous handlers
              $(document).on('click', '.row-checkbox', function () {
                       const id = parseInt($(this).data('tcidcheck'));
                       const isChecked = $(this).prop('checked');

                       if (isChecked) {
                           if (!selectedCurrencies.includes(id)) {
                               selectedCurrencies.push(id);
                           }
                       } else {
                           const index = selectedCurrencies.indexOf(id);
                           if (index !== -1) {
                               selectedCurrencies.splice(index, 1);
                           }
                       }

                       const allChecked = $('.row-checkbox:visible').length > 0 &&
                           $('.row-checkbox:visible:not(:checked)').length === 0;
                           $('#selectAll').prop('checked', allChecked);

                       console.log("Selected expense heads:", selectedCurrencies);
               });
        }

             //Ready
        $(document).ready(function(){
                //Form Section Start 
         
            $('#currencyFormView').load('/CurrencyType/CurrencyForm', function () {
             
                loadNextCurrencyId(); 
            });

                          // Handle delete button click
             $(document).on("click",".deleteBtn", function (e) {
                                e.preventDefault();
                                deleteCurrencyList();
             });

                            // Handle clear button
              $(document).on("click",".resetBtn", function () {
                                console.log("Clerar Button Clicked")
                                clearForm();
              });

                            // Handle print button
              $(".printBtn").on("click", function (e) {
                                e.preventDefault();
                                printForm();
              });

                            // Handle favorite button
              $(".favBtn").on("click", function () {
                                toggleFavorite();
              });


             $(document).on("submit", "#currencyForm", function(e){
                        e.preventDefault();
                        saveCurrency();
             });

                    //  List Section Start 
             $('#currencyListView').load('/CurrencyType/CurrencyList');
             loadCurrencies(currentPage, pageSize, currentSortColumn, currentSortDirection);

                    // Search functionality with debounce
            $(document).on('input','.searchInput', debounce(function () {
                const searchValue = $('.searchInput').val();
                loadCurrencies(1, pageSize, currentSortColumn, currentSortDirection, searchValue); // Reset to page 1 when searching
            }, 500)); 

                   // Static pagination buttons
            $(document).on('click', '#firstPage',function () {
                currentPage = 1;
                loadCurrencies(currentPage, pageSize, currentSortColumn, currentSortDirection);
            });

            $(document).on('click', '#prevPage', function () {
                console.log("previous page called on currency page ");
                if (currentPage > 1) {
                    currentPage--;
                    loadCurrencies(currentPage, pageSize, currentSortColumn, currentSortDirection);
                }
            });

            $(document).on('click', '#nextPage', function () {
                currentPage++;
                loadCurrencies(currentPage, pageSize, currentSortColumn, currentSortDirection);
            });

            $(document).on('click', '#lastPage',function () {
                // Get total pages from the pagination info
                const totalPages = parseInt($('#paginationInfo').text().split(' ')[3]) || 1;
                loadCurrencies(totalPages, pageSize, currentSortColumn, currentSortDirection);
            });

            // Change page size
            $(document).on('change','.pageSize',function () {
                pageSize = parseInt($(this).val());
                loadCurrencies(1, pageSize, currentSortColumn, currentSortDirection); // Reset to the first page on size change
            });

            // Sortable columns click handler
            $(document).on('click','.sortable',function() {
                const column = $(this).data('column');

                // If clicking on the same column, toggle direction
                // Otherwise, set the new column with 'asc' direction
                if (column === currentSortColumn) {
                    currentSortDirection = currentSortDirection === 'asc' ? 'desc' : 'asc';
                } else {
                    currentSortColumn = column;
                    currentSortDirection = 'asc';
                }

                // Reload data with new sort settings
                loadCurrencies(1, pageSize, currentSortColumn, currentSortDirection);
            });

              // Initial binding of checkbox handlers
            bindCheckboxHandlers();

        });
  
//$(document).on('change', '#currencyName', function () {
//    let currencyNameValue = $('#currencyName').val();
//    $('#searchInput').val(currencyNameValue);

//    console.log(`Search Value: ${$('#searchInput').val()}`);
//    loadCurrencies(page = 1, pageSize = 10, sortColumn = currentSortColumn, sortDirection = currentSortDirection, searchTerm=currencyNameValue);

//});
// Show decimal format
//function showDecimalFormat(decimalPlaceValue) {
//    let positiveFormat = "1";

//    if (decimalPlaceValue > 0) {
//        positiveFormat += "." + "0".repeat(decimalPlaceValue);
//    }

//    $('#showFormat').val(positiveFormat);
//    $('#negativeFormat').val("-" + positiveFormat);
//}

//// Attach input event
//$(document).on('input', '#decimalPlaces', function () {
//    let decimalValue = parseInt($(this).val());

//    if (isNaN(decimalValue)) {
//        $('#showFormat').val("");
//        $('#negativeFormat').val("");
//        return;
//    }

//    showDecimalFormat(decimalValue);
//});