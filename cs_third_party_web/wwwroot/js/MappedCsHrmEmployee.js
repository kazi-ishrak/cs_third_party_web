
function ShowMappedCsHrmEmployeeTable() {

    $('#mappedCsHrmTable').DataTable({
        "processing": true,
        "language": {
            "processing": '<i class="fa fa-spinner fa-spin fa-2x fa-fw text-primary"></i><span class="sr-only">Loading...</span>'
        },
        "serverSide": true,
        "order": [3, "desc"],
        //deferRender: true,               //For Client Side Fast Loading
        "responsive": true,

        "ajax": {
            "url": "/api/CsHrmMap/csHrmMap",
            "type": "POST",  //Important
            //"dataSrc": "",               //For Client Side
            "data": function (d) {
                // Add parameters from input fields
                d.ProjectId = $('#MappingsProjectSelect').val();
                return d;
            },
            "contentType": "application/x-www-form-urlencoded"
        },

        "columns": [
            { "data": "project_id", "name": "project_id" },
            { "data": "project_name", "name": "project_name" },
            { "data": "hrm_id", "name": "hrm_id" },
            { "data": "hrm_office_id", "name": "hrm_office_id" },
            { "data": "cs_id", "name": "cs_id" },

            {
                "data": "created_at", "orderable": true,
                "render": function (data, type, row, meta) {

                    var dateObj = new Date(data);
                    var day = ("0" + dateObj.getDate()).slice(-2);
                    var month = ("0" + (dateObj.getMonth() + 1)).slice(-2);
                    var year = dateObj.getFullYear().toString().slice(-2);
                    var hours = ("0" + dateObj.getHours()).slice(-2);
                    var hours = dateObj.getHours();
                    var ampm = hours >= 12 ? "PM" : "AM";
                    hours = hours % 12;
                    hours = hours ? hours : 12;
                    var minutes = ("0" + dateObj.getMinutes()).slice(-2);
                    var seconds = ("0" + dateObj.getSeconds()).slice(-2);
                    var formattedDatetime = day + "." + month + "." + year + " " + hours + ":" + minutes + ":" + seconds + " " + ampm;

                    return formattedDatetime;
                },
            },
            {
                "data": "updated_at", "orderable": true,
                "render": function (data, type, row, meta) {

                    var dateObj = new Date(data);
                    var day = ("0" + dateObj.getDate()).slice(-2);
                    var month = ("0" + (dateObj.getMonth() + 1)).slice(-2);
                    var year = dateObj.getFullYear().toString().slice(-2);
                    var hours = ("0" + dateObj.getHours()).slice(-2);
                    var hours = dateObj.getHours();
                    var ampm = hours >= 12 ? "PM" : "AM";
                    hours = hours % 12;
                    hours = hours ? hours : 12;
                    var minutes = ("0" + dateObj.getMinutes()).slice(-2);
                    var seconds = ("0" + dateObj.getSeconds()).slice(-2);
                    var formattedDatetime = day + "." + month + "." + year + " " + hours + ":" + minutes + ":" + seconds + " " + ampm;

                    return formattedDatetime;
                },
            }

        ],

        select: true,
        //stateSave: true,

        //scrollY: 300,
        //scroller: {
        //    loadingIndicator: true
        //},

        "columnDefs": [
            { "className": "dt-center", "targets": "_all" }
        ],

        //"columnDefs": [{
        //    "targets": -1,
        //    "data": null,
        //    "render": function (data, type, row, meta) {
        //        return '<a href="EditMold/'+ meta.row +'" class="btn btn-primary"><i class="fa fa-pen" aria-hidden="true"></i></a>';
        //    }
        //}]
    });
}

$(function () {
    GetProjectIdAndNameAtMappingPage();
    ShowMappedCsHrmEmployeeTable();
});
$('#filterButtonMapping').on('click', function () {
    $('#mappedCsHrmTable').DataTable().destroy(); // Destroy existing DataTable instance
    ShowMappedCsHrmEmployeeTable() // Reinitialize DataTable
});

function GetProjectIdAndNameAtMappingPage() {
    var endpointUrl = '/api/Projects/idAndName';
    $.ajax({
        url: endpointUrl,
        method: 'GET',
        success: function (data) {
            if (data && Array.isArray(data)) {
                var $dropdown = $('#MappingsProjectSelect');
                $dropdown.empty();
                $dropdown.append('<option value="">Select A project </option>');

                $.each(data, function (index, project) {
                    $dropdown.append(
                        $('<option></option>').val(project.project_id).text(project.name));
                });
            }
        }
    });
}