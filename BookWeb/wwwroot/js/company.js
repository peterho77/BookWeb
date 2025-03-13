var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#companyTable').DataTable({
        ajax: { url: '/Admin/Company/GetAll' },
        columns: [
            { data: 'name' , "width":'15%' },
            { data: 'streetAddress', "width": '30%'  },
            { data: 'city', "width": '15%' },
            { data: 'phoneNumber', "width": '15%' },
            {
                data: 'id',
                "render": function (data) {
                    return `
                        <div class="btn-group w-100" role="group">
							<a href="/admin/company/upsert?id=${data}" class="btn btn-outline-primary mx-2 form-control">Edit</a>
							<a onClick=Delete('/admin/company/delete/${data}') class="btn btn-danger mx-2 form-control">Delete</a>
						</div>
                    `
                },
                "width": '25%'
            }
        ]
    });
}

function Delete(url) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    dataTable.ajax.reload();
                    Swal.fire({
                        title: "Deleted!",
                        text: data.message,
                        icon: data.icon
                    });
                }
            })
        }
    });
}

