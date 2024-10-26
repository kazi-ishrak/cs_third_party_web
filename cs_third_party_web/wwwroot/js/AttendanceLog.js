$(function SetDefaultDate() {
    var today = new Date();
    var dd = String(today.getDate()).padStart(2, '0');
    var mm = String(today.getMonth() + 1).padStart(2, '0'); // January is 0!
    var yyyy = today.getFullYear();

    today = yyyy + '-' + mm + '-' + dd;
    $('#startDate').val(today);
    $('#endDate').val(today);
});
function ShowAttendanceLogs() {

    $('#attendanceTable').DataTable({
        "processing": true,
        "language": {
            "processing": '<i class="fa fa-spinner fa-spin fa-2x fa-fw text-primary"></i><span class="sr-only">Loading...</span>'
        },
        "serverSide": true,
        "order": [6, "desc"],
        //deferRender: true,               //For Client Side Fast Loading
        "responsive": true,

        "ajax": {
            "url": "/api/AttendanceLog",
            "type": "POST",  //Important
            //"dataSrc": "",               //For Client Side
            "data": function (d) {
                // Add parameters from input fields
                d.ProjectId = $('#projectSelect').val();
                d.StartDate = $('#startDate').val();
                d.EndDate = $('#endDate').val();
                return d;
            },
            "contentType": "application/x-www-form-urlencoded"
        },

        "columns": [
            { "data": "project_id", "name": "project_id" },
            { "data": "project_name", "name": "project_name" },
            { "data": "person_id", "name": "person_id" },
            { "data": "person_identifier", "name": "person_identifier" },

            {
                "data": "hrm_office_id", "orderable": true,
                "render": function (data, type, row, meta) {
                    if (row.hrm_identifier_enabled)
                        return data;
                    else
                        return '<span> <i class="fa fa-ban text-danger" aria-hidden="true"></i> </span>';
                },

            },
            { "data": "device_identifier", "name": "device_identifier" },
            {
                "data": "log_time", "orderable": true,
                "render": function (data, type, row, meta) {
                    
                    return formatDatetime(data);
                },
            },
            {
                "data": "sync_time", "orderable": true,
                "render": function (data, type, row, meta) {
                    return formatDatetime(data);
                },
            },
            {
                "data": "created_at", "orderable": true,
                "render": function (data, type, row, meta) {

                    return formatDatetime(data);
                },
            },
            {
                "data": "client_sync_time", "orderable": true,
                "render": function (data, type, row, meta) {
                    if (data != null) {
                        return formatDatetime(data);
                    }
                    else {
                        return " ";
                    }
                        
                      
                },
            },
            {
                "data": "flag", "orderable": true,
                "render": function (data, type, row, meta) {
                    if (data == true) {
                        return '<span> <i class="fa fa-check-circle text-success" aria-hidden="true"></i> </span>';
                    }
                    else {
                        return '<span> <i class="fa fa-times-circle text-danger" aria-hidden="true"></i> </span>';
                    }
                },
            },
            { "data": "remarks", "name": "remarks" },
           

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
    ShowAttendanceLogs();
});

$('#filterButton').on('click', function () {
    $('#attendanceTable').DataTable().destroy(); // Destroy existing DataTable instance
    ShowAttendanceLogs(); // Reinitialize DataTable
});

/*$('#filterButton').on('click', ShowAttendanceLogs);*/

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


