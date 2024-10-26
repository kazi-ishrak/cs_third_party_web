//$(document).ready(function () {
//    $('#myTable').DataTable();
//});

$(document).ready(function () {

    $('#DataTable_Devices').DataTable({
        "processing": true,
        "language": {
            //"processing": '<i class="fa fa-spinner fa-spin fa-2x fa-fw text-primary"></i><span class="sr-only">Loading...</span> '
        },
        "serverSide": true,
        "order": [0, "asc"],
        //deferRender: true,               //For Client Side Fast Loading
        "responsive": true,

        "ajax": {
            "url": "/Home/GetDevices",
            "type": "POST",  //Important
            //"dataSrc": "",               //For Client Side
        },

        "columns": [
            { "data": "dev_id", "name": "dev_id" },
            { "data": "name", "name": "name" },
            {
                "data": "regtime", "orderable": true,
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
            //{
            //    "data": "Service", "orderable": false,
            //    "render": function (data, type, row, meta) {
            //        if (data == true) {
            //            return '<span> <i class="fa fa-check-circle text-success" aria-hidden="true"></i> </span>'
            //        }
            //        else {
            //            return '<span> <i class="fa fa-times-circle text-danger" aria-hidden="true"></i> </span>'
            //        }
            //    },
            //},
            //{
            //    "data": "Notify", "orderable": false,
            //    "render": function (data, type, row, meta) {
            //        if (data == true) {
            //            return '<span> <i class="fa fa-check-circle text-success" aria-hidden="true"></i> </span>'
            //        }
            //        else {
            //            return '<span> <i class="fa fa-times-circle text-danger" aria-hidden="true"></i> </span>'
            //        }
            //    },
            //},
            //{
            //    "data": "Status", "orderable": false,
            //    "width": "15%",
            //    "render": function (data, type, row, meta) {
            //        if (data == true) {
            //            return '<a href="#" onclick=UserUpdate(' + row.ID + ') class="btn btn-primary" data-toggle="modal" data-target="#updateModal"><i class="fa fa-pen fa-sm" aria-hidden="true"></i></a>&nbsp; <a href="#" onclick="UserDisable(' + row.ID + ')" class="btn btn-danger"><i class="fa fa-ban fa-sm" aria-hidden="true"></i></a>';
            //        }
            //        else {
            //            return '<a href="#" onclick=UserUpdate(' + row.ID + ') class="btn btn-primary" data-toggle="modal" data-target="#updateModal"><i class="fa fa-pen fa-sm" aria-hidden="true"></i></a>&nbsp; <a href="#" onclick="UserEnable(' + row.ID + ')" class="btn btn-success"><i class="fa fa-check fa-sm" aria-hidden="true"></i></a>';
            //        }
            //    },
            //},
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

    ////
    $('#DataTable_Logs').DataTable({
        "processing": true,
        "language": {
            //"processing": '<i class="fa fa-spinner fa-spin fa-2x fa-fw text-primary"></i><span class="sr-only">Loading...</span> '
        },
        "serverSide": true,
        "order": [0, "asc"],
        //deferRender: true,               //For Client Side Fast Loading
        "responsive": true,

        "ajax": {
            "url": "/Home/GetLogs",
            "type": "POST",  //Important
            //"dataSrc": "",               //For Client Side
        },

        "columns": [
            { "data": "user_id", "name": "user_id" },
            { "data": "dev_id", "name": "dev_id" },
            {
                "data": "verify_mode", "orderable": true,
                "render": function (data, type, row, meta) {
                    data = data.replace(/[/g, '').replace(/]/g, '').replace(/"/g, '');
                    return data;
                },
            },
            {
                "data": "io_time", "orderable": true,
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
    });

    ////
    $('#DataTable_Enrolls').DataTable({
        "processing": true,
        "language": {
            //"processing": '<i class="fa fa-spinner fa-spin fa-2x fa-fw text-primary"></i><span class="sr-only">Loading...</span> '
        },
        "serverSide": true,
        "order": [0, "asc"],
        //deferRender: true,               //For Client Side Fast Loading
        "responsive": true,

        "ajax": {
            "url": "/Home/GetEnrolls",
            "type": "POST",  //Important
            //"dataSrc": "",               //For Client Side
        },

        "columns": [
            { "data": "user_id", "name": "user_id" },
            {
                "data": "backup_number", "orderable": true,
                "render": function (data, type, row, meta) {

                    var number = parseInt(data);

                    if (number === 12) {
                        return "Face";
                    }
                    else if (number >= 0 && number < 10) {
                        var result = "FP";
                        for (i = 0; i < 10; i++) {
                            if (i == number) {
                                result = result + "-" + (i + 1);
                            }
                        }
                        return result;
                    }
                    else if (number === 11) {
                        return "Card";
                    }
                    else if (number === 10) {
                        return "Password";
                    }
                    else if (number === 50) {
                        return "Photo";
                    }
                    else {
                        return "";
                    }
                },
            },
            {
                "data": "regtime", "orderable": true,
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
    });
});