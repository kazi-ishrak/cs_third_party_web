$(function SetDefaultDate() {
    var today = new Date();
    var dd = String(today.getDate()).padStart(2, '0');
    var mm = String(today.getMonth() + 1).padStart(2, '0'); // January is 0!
    var yyyy = today.getFullYear();

    today = yyyy + '-' + mm + '-' + dd;
    $('#LogReqstartDate').val(today);
    $('#LogReqendDate').val(today);
});

function ShowLogRequest() {

    $('#LogRequestTable ').DataTable({
        "processing": true,
        "language": {
            "processing": '<i class="fa fa-spinner fa-spin fa-2x fa-fw text-primary"></i><span class="sr-only">Loading...</span>'
        },
        "serverSide": true,
        "order": [12, "desc"],
        //deferRender: true,               //For Client Side Fast Loading
        "responsive": true,

        "ajax": {
            "url": "/LogRequest/all",
            "type": "POST",
            "data": function (d) {
                // Add parameters from input fields
                d.ProjectId = $('#LogReqprojectSelect').val();
                d.StartDate = $('#LogReqstartDate').val();
                d.EndDate = $('#LogReqendDate').val();
                return d;
            },
            "contentType": "application/x-www-form-urlencoded"
        },

        "columns": [
            { "data": "project_id", "name": "project_id" },
            { "data": "project_name", "name": "project_id" },
            { "data": "code", "name": "code" },
            { "data": "message", "name": "message" },
            {
                "data": "start_date", "orderable": true,
                "render": function (data, type, row, meta) {
                    if (data == null) {
                        return '<span> <i class="fa fa-ban text-danger" aria-hidden="true"></i> </span>';
                    }
                    return formatDatetime(data);
                },
            },
            {
                "data": "end_date", "orderable": true,
                "render": function (data, type, row, meta) {
                    if (data == null) {
                        return '<span> <i class="fa fa-ban text-danger" aria-hidden="true"></i> </span>';
                    }
                    return formatDatetime(data);
                },
            },
            { "data": "api_token", "name": "api_token" },
            { "data": "criteria", "name": "criteria" },
            { "data": "per_page", "name": "per_page" },
            { "data": "page", "name": "page" },
            { "data": "order_key", "name": "order_key" },
            { "data": "order_direction", "name": "order_direction" },
            {
                "data": "created_at", "orderable": true,
                "render": function (data, type, row, meta) {
                    if (data == null) {
                        return '<span> <i class="fa fa-ban text-danger" aria-hidden="true"></i> </span>';
                    }
                    return formatDatetime(data);
                },
            },
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
    GetProjectIdAndNameAtLogRequestPage();
    ShowLogRequest();
});
$('#filterButtonlogReq').on('click', function () {
    $('#LogRequestTable').DataTable().destroy(); // Destroy existing DataTable instance
    ShowLogRequest(); // Reinitialize DataTable
});
function GetProjectIdAndNameAtLogRequestPage() {
    var endpointUrl = '/api/Projects/idAndName';
    $.ajax({
        url: endpointUrl,
        method: 'GET',
        success: function (data) {
            if (data && Array.isArray(data)) {
                var $dropdown = $('#LogReqprojectSelect');
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


function formatDatetime(data) {
    var dateObj = new Date(data);
    var day = ("0" + dateObj.getDate()).slice(-2);
    var month = ("0" + (dateObj.getMonth() + 1)).slice(-2);
    var year = dateObj.getFullYear().toString().slice(-2);
    var hours = ("0" + dateObj.getHours()).slice(-2);
    var ampm = hours >= 12 ? "PM" : "AM";
    hours = hours % 12;
    hours = hours ? hours : 12;
    var minutes = ("0" + dateObj.getMinutes()).slice(-2);
    var seconds = ("0" + dateObj.getSeconds()).slice(-2);
    return day + "." + month + "." + year + " " + hours + ":" + minutes + ":" + seconds + " " + ampm;
}