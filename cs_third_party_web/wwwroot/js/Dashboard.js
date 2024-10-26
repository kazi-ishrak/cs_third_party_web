$(function () {

    $('#DataTable_Dashboard').DataTable({
        "processing": true,
        "language": {
            "processing": '<i class="fa fa-spinner fa-spin fa-2x fa-fw text-primary"></i><span class="sr-only">Loading...</span>'
        },
        "serverSide": true,
        "order": [3, "desc"],
        //deferRender: true,               //For Client Side Fast Loading
        "responsive": true,

        "ajax": {
            "url": "/Dashboard/table",
            "type": "POST",  //Important
            //"dataSrc": "",               //For Client Side
        },

        "columns": [
            { "data": "project_id", "name": "project_id" },
            { "data": "project_name", "name": "project_name" },
            { "data": "project_type", "name": "project_name" },
            {
                "data": "client_sync_time", "orderable": true,
                "render": function (data, type, row, meta)
                {
                    if (data == null) {
                        return '<span> <i class="fa fa-ban text-danger" aria-hidden="true"></i> </span>';
                    }
                    return formatDatetime(data);
                },
            },
            { "data": "logs_entered_today", "name": "logs_entered_today" },
            { "data": "logs_synced_today", "name": "logs_synced_today" },      
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
});


function GetCardInfos() {
    var endpointUrl = '/Dashboard/cards';
    $.ajax({
        url: endpointUrl,
        method: 'GET',
        success: function (data) {
            $('#TotalProjects').text(data[0]);
            $('#TotalLogs').text(data[1]);
            $('#syncedLogs').text(data[2]);
        }
    });
}
$(function () {
    GetCardInfos();
});

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