$(function () {

    $('#DataTable_Projects').DataTable({
        "processing": true,
        "language": {
            "processing": '<i class="fa fa-spinner fa-spin fa-2x fa-fw text-primary"></i><span class="sr-only">Loading...</span>'
        },
        "serverSide": true,
        "order": [7, "desc"],
        //deferRender: true,               //For Client Side Fast Loading
        "responsive": true,

        "ajax": {
            "url": "/api/Projects/projectList",
            "type": "POST",  //Important
            //"dataSrc": "",               //For Client Side
        },

        "columns": [
            { "data": "project_id", "name": "project_id" },
            { "data": "code", "name": "code" },
            { "data": "name", "name": "name" },
            { "data": "type_name", "name": "type_name"},
            { "data": "organization", "name": "organization" },
            { "data": "api_token", "name": "api_token" },
            {
                "data": "hrm_identifier", "orderable": true,
                "render": function (data, type, row, meta) {
                    if (data == true) {
                        return '<span> <i class="fa fa-check-circle text-success" aria-hidden="true"></i> </span>';
                    }
                    else {
                        return '<span> <i class="fa fa-times-circle text-danger" aria-hidden="true"></i> </span>';
                    }
                },
            },
            {
                "data": "created_at", "orderable": true,
                "render": function (data, type, row, meta) {
                    return formatDatetime(data);
                },
            },
            {
                "data": "updated_at", "orderable": true,
                "render": function (data, type, row, meta) {
                    return formatDatetime(data);
                },
            },
            {
                "data": null,
                "width": "10%",
                "render": function (data, type, row, meta) {
                    return '<a href="#" onclick=RowEdit(' + row.id + ') class="btn btn-outline-light btn-sm text-primary" > <i class="fa fa-pencil text-warning" aria-hidden="true"></i></a>';
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
});

$('#openModalButton').on('click', function () {
    $('#addProjectModal').modal('show');
});

$('#submitProjectButton').on('click', HandleFormSubmit);
$('#updateProjectButton').on('click', HandleUpdateFromSubmit);
function RowEdit(rowid) { 

    $.ajax({
        url: `/api/Projects/${rowid}`,
        method: 'GET',
        contentType: 'application/json',

        success: function (data) {
            console.log(data);

            $('#projectIdUpdate').val(data[0].project_id);
            $('#projectCodeUpdate').val(data[0].code);
            $('#projectNameUpdate').val(data[0].name);
            $('#projectTypeIdUpdate').val(data[0].type_id);
            $('#organizationUpdate').val(data[0].organization);
            $('#apiTokenUpdate').val(data[0].api_token);
            $('#hrmEnabledUpdate').val(data[0].hrm_identifier ? 'Yes' : 'No');
            $('#projectIdHidden').val(data[0].id);
            $('#rowEditModal').modal('show');
        },
       
    });
}
function HandleUpdateFromSubmit(e) {
    e.preventDefault();
    var updateFormData = {
        id: $('#projectIdHidden').val().toString(),
        project_id: $('#projectIdUpdate').val().toString(),
        type_id: $('#projectTypeIdUpdate').val(),
        code: $('#projectCodeUpdate').val(),    
        name: $('#projectNameUpdate').val(),
        organization: $('#organizationUpdate').val(),
        api_token: $('#apiTokenUpdate').val(),
        hrm_identifier: $('#hrmEnabledUpdate').val() === 'Yes'
    };
    if (!updateFormData.project_id || !updateFormData.name || !updateFormData.api_token || !updateFormData.type_id) {
        alert('Please fill in all required fields.');
        $('#projectIdUpdate').addClass('required-field');
        $('#apiTokenUpdate').addClass('required-field');
        $('#projectNameUpdate').addClass('required-field');
        $('#projectTypeIdUpdate').addClass('required-field');
        return;
    }
    UpdateProjectData(updateFormData);
}


function UpdateProjectData(formData) {
    $.ajax({
        url: '/api/Projects/update',

        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function (response) {
            alert("Project successfully Updated.");
            $('#rowEditModal').modal('hide');
            $('#DataTable_Projects').DataTable().ajax.reload(null, false);
            console.log("API response:", response);
        },
        error: function (xhr, status, error) {
            console.error("Error Updating project:", xhr.responseText);
            alert("Failed to Update project: " + xhr.responseText);
        }
    });
}
function HandleFormSubmit(e) {
    e.preventDefault();
    var formdata = {
        project_id: $('#projectId').val().toString(),
        type_id: $('#projectTypeSelect').val().toString(),
        code: $('#projectCode').val(),
        name: $('#projectName').val(),
        organization: $('#organization').val(),
        api_token: $('#apiToken').val(),
        hrm_identifier: $('#hrmEnabled').val() === 'Yes'
    };
    if (!formdata.project_id || !formdata.name || !formdata.api_token || !formdata.type_id) {
        alert('Please fill in all required fields.');
        $('#projectId').addClass('required-field');
        $('#apiToken').addClass('required-field');
        $('#projectName').addClass('required-field');
        $('#projectTypeSelect').addClass('required-field');
        console.log(formdata.type_id);
        return;
    }

    InsertAProject(formdata);
}

function InsertAProject(formData) {

    $.ajax({
        url: '/api/Projects/insert',

        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function (response) {
            console.log("API response:", response);
            alert("Project successfully Created.");
            $('#addProjectModal').modal('hide');
            
            $('#DataTable_Projects').DataTable().ajax.reload(null, false);
            clearFormFields();
        },
        error: function (xhr, status, error) {
            console.error("Error inserting project:", xhr.responseText);
            alert("Failed to add project: " + xhr.responseText);
        }
    });
}

function clearFormFields() {
    $('input[type="text"]').val('');
    $('#hrmEnabled').val('');
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