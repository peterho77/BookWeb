var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
    }
    else if (url.includes("pending")) {
        loadDataTable("pending");
    }
    else if (url.includes("approved")) {
        loadDataTable("approved");
    }
    else if (url.includes("completed")) {
        loadDataTable("completed");
    }
    else {
        loadDataTable("all");
    }
});

function loadDataTable(status) {
    dataTable = $('#orderTable').DataTable({
        ajax: { url: '/Admin/Order/GetAll?status=' + status},
        columns: [
            { data: 'id' , "width":'5%' },
            { data: 'name', "width": '10%'  },
            { data: 'phoneNumber', "width": '15%' },
            { data: 'applicationUser.email', "width": '15%' },
            { data: 'orderStatus', "width": '20%' }, 
            { data: 'orderTotal', "width": '20%' },
            {
                data: 'id',
                "render": function (data) {
                    return `
                        <div class="btn-group w-75" role="group">
							<a href="/Admin/Order/Details?orderId=${data}" class="btn btn-outline-primary mx-2 form-control">Detail</a>
						</div>
                    `
                },
                "width": '15%'
            }
        ]
    });
}


