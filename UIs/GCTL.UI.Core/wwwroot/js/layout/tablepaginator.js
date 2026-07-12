function setupPagination(config) {
    let currentPage = 1;
    let pageSize = 5;
    let currentSortColumn = config.defaultSortColumn || '';
    let currentSortOrder = config.defaultSortOrder || '';

    const $searchInput = $(config.searchInputSelector);
    const $pageSizeSelect = $(config.pageSizeSelector);
    const $prevBtn = $(config.prevBtnSelector);
    const $nextBtn = $(config.nextBtnSelector);
    const $tableBody = $(config.tableBodySelector);
    const $paginationLinks = $(config.paginationLinksSelector);

    $pageSizeSelect.on('change', function () {
        const selectedSize = $(this).val();
        if (selectedSize) {
            pageSize = parseInt(selectedSize, 10);
            currentPage = 1;
            loadTableData();
        }
    });

    $searchInput.on("input", function () {
        currentPage = 1;
        loadTableData();
    });

    $prevBtn.on('click', function () {
        if (currentPage > 1) {
            currentPage--;
            loadTableData();
        }
    });

    $nextBtn.on('click', function () {
        currentPage++;
        loadTableData();
    });

    $(document).on('click', `${config.sortableHeaderSelector}`, function () {
        const column = $(this).data('sort');
        if (currentSortColumn === column) {
            currentSortOrder = currentSortOrder === 'asc' ? 'desc' : 'asc';
        } else {
            currentSortColumn = column;
            currentSortOrder = 'asc';
        }
        loadTableData();
        updateSortingIndicator();
    });

    $(document).on('click', `${config.paginationLinksSelector} .page-btn`, function () {
        const page = $(this).data('page');
        currentPage = page;
        loadTableData();
    });

    function updateSortingIndicator() {
        $(`${config.sortableHeaderSelector}`).each(function () {
            const $th = $(this);
            const column = $th.data('sort');
            $th.find('.sort-icon').remove();
            const iconClass = column === currentSortColumn
                ? (currentSortOrder === 'asc' ? 'fa-sort-up' : 'fa-sort-down')
                : 'fa-sort';
            $th.append(`<span class="sort-icon ms-2"><i class="fas ${iconClass} small text-muted"></i></span>`);
        });
    }

    function loadTableData() {
        $.ajax({
            url: config.url,
            method: 'GET',
            data: {
                pageNumber: currentPage,
                pageSize: pageSize,
                searchTerm: $searchInput.val(),
                sortColumn: currentSortColumn,
                sortOrder: currentSortOrder
            },
            success: function (response) {
                $tableBody.empty();
                if (response.data && response.data.length > 0) {
                    config.renderRow(response.data, currentPage, pageSize, currentSortOrder);
                } else {
                    $tableBody.append('<tr><td colspan="10" class="text-center">No data available</td></tr>');
                }

                if (response.paginationInfo) {
                    updatePagination(response.paginationInfo);
                    if (config.paginationSummarySelector) {
                        $(config.paginationSummarySelector).text(
                            `Showing ${response.paginationInfo.startItem} to ${response.paginationInfo.endItem} of ${response.paginationInfo.totalItems}`
                        );
                    }
                }
            }
        });
    }

    function updatePagination(paginationInfo) {
        const { pageNumbers, currentPage, totalPages } = paginationInfo;
        $paginationLinks.empty();

        const windowSize = 1;

        const createPageButton = (page) => `
            <li class="page-item ${page === currentPage ? 'active' : ''}">
                <button class="page-link page-btn" data-page="${page}">${page}</button>
            </li>`;

        const addEllipsis = () => '<li class="page-item disabled"><span class="page-link">...</span></li>';

        if (currentPage > windowSize + 1) {
            $paginationLinks.append(createPageButton(1), addEllipsis());
        }

        const startPage = Math.max(1, currentPage - windowSize);
        const endPage = Math.min(totalPages, currentPage + windowSize);
        for (let i = startPage; i <= endPage; i++) {
            $paginationLinks.append(createPageButton(i));
        }

        if (currentPage < totalPages - windowSize) {
            $paginationLinks.append(addEllipsis(), createPageButton(totalPages));
        }

        $prevBtn.prop('disabled', currentPage === 1);
        $nextBtn.prop('disabled', currentPage === totalPages);
    }

    // Initial load
    loadTableData();
}




setupPagination({
    url: gridUrl,
    defaultSortColumn: 'BankIssuedLetterID',
    defaultSortOrder: 'desc',
    searchInputSelector: '#actionTaken-searchInput',
    pageSizeSelector: '#actionTaken-pageSizeSelect',
    prevBtnSelector: '#actionTaken-prevPageBtn',
    nextBtnSelector: '#actionTaken-nextPageBtn',
    tableBodySelector: '#actionTaken-tBody',
    paginationLinksSelector: '#actionTaken-paginationLinks',
    paginationSummarySelector: '#actionTaken-paginationInfo',
    sortableHeaderSelector: 'th.sort',
    renderRow: function (data, currentPage, pageSize, sortOrder) {
        data.forEach(function (item, index) {
            let rowIndex = sortOrder === 'asc'
                ? (currentPage - 1) * pageSize + index + 1
                : item.totalItems - ((currentPage - 1) * pageSize + index);

            $("#actionTaken-tBody").append(`
                        <tr>
                            <td><input type="checkbox" class="actionTaken-selectItem" data-id="${item.actionTakenID}" /></td>
                            <td>${rowIndex}</td>
                            <td>${item.actionTakenName}</td>
                            <td>
                                <a class="actionTaken-bulkEdit" data-id="${item.actionTakenID}"><i class="fas fa-edit"></i></a>
                                <a class="actionTaken-bulkDelete" data-id="${item.actionTakenID}"><i class="fas fa-trash text-danger"></i></a>
                            </td>
                        </tr>
                    `);
        });
    }
});





//