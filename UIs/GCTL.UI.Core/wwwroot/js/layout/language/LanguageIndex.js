$(document).ready(function () {

    $(document).ready(function () {
        $("#languageCode").change(function () {
            let selectedLang = $(this).val();
            alert("Selected Language: " + selectedLang);

            if (selectedLang) {
                $.ajax({
                    url: "/Language/GetTranslationsTable",
                    type: "GET",
                    data: { languageCode: selectedLang },
                    success: function (data) {
                        alert()
                        populateTable(data);
                    },
                    error: function () {
                        toastr.error("Failed to load translations.");
                    }
                });
            }
        });
    });


    $("#searchInput").on("keyup", function () {
        let searchText = $(this).val().toLowerCase();
        let filteredData = translationsData.filter(item =>
            item.englishText.toLowerCase().includes(searchText) ||
            item.translatedText.toLowerCase().includes(searchText)
        );

        currentPage = 1; // Reset to first page
        translationsData = filteredData;
        displayPage(currentPage);
        setupPagination();
    });

   
});




let translationsData = [];
let currentPage = 1;
const rowsPerPage = 10;

function populateTable(data) {
    translationsData = data; // Store the full dataset
    displayPage(currentPage);
    setupPagination();
}

function displayPage(page) {
    let tbody = $("#langTable");
    tbody.empty();

    let start = (page - 1) * rowsPerPage;
    let end = start + rowsPerPage;
    let paginatedData = translationsData.slice(start, end);

    $.each(paginatedData, function (index, item) {
        let row = `<tr>
                        <td>${start + index + 1}</td>
                        <td>${item.englishText}</td>
                        <td>${item.translatedText}</td>
                        <td>
                            <button class="btn btn-primary edit-btn" data-id="${item.id}">Edit</button>
                        </td>
                    </tr>`;
        tbody.append(row);
    });
}

function setupPagination() {
    let totalPages = Math.ceil(translationsData.length / rowsPerPage);
    let paginationContainer = $("#pagination");

    paginationContainer.empty();
    for (let i = 1; i <= totalPages; i++) {
        paginationContainer.append(`<button class="btn btn-secondary page-btn" data-page="${i}">${i}</button>`);
    }

    $(".page-btn").click(function () {
        currentPage = $(this).data("page");
        displayPage(currentPage);
    });
}