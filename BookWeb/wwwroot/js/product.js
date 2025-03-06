var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#one').DataTable({
        ajax: { url: '/Admin/Product/GetAll' },
        columns: [
            { data: 'title' , "width":'10%' },
            { data: 'isbn', "width": '10%'  },
            { data: 'listPrice', "width": '10%' },
            { data: 'author', "width": '10%' },
            { data: 'category.name', "width": '10%' },
            {
                data: 'id',
                "render": function (data) {
                    return `
                        <div class="btn-group w-100" role="group">
							<a href="/admin/product/upsert?id=${data}" asp-controller="Product" asp-action="Upsert" class="btn btn-outline-primary mx-2 form-control">Edit</a>
							<a onClick=Delete('/admin/product/delete/${data}') class="btn btn-danger mx-2 form-control">Delete</a>
						</div>
                    `
                },
                "width": '20%'
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

