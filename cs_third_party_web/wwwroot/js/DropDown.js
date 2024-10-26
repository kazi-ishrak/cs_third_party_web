function GetProjectIdAndName() {
    var endpointUrl = '/api/Projects/idAndName';
    $.ajax({
        url: endpointUrl,
        method: 'GET',
        success: function (data) {
            if (data && Array.isArray(data)) {
                var $dropdown = $('#projectSelect');
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

function GetProjectTypeAndName() {
    var endpointUrl = 'api/Projects/type';
    $.ajax({
        url: endpointUrl,
        method: 'GET',
        success: function (data) {
            if (data && Array.isArray(data)) {
                var InsertProjectType = $('#projectTypeSelect');
                var UpdateProjectType = $('#projectTypeIdUpdate');
              
                InsertProjectType.empty();
                UpdateProjectType.empty();
                InsertProjectType.append('<option value="">Select Project Type</option>');
                UpdateProjectType.append('<option value="">Select Project Type</option>');
                $.each(data, function (index, project) {
                    InsertProjectType.append($('<option></option>').val(project.id).text(project.name));
                    UpdateProjectType.append($('<option></option>').val(project.id).text(project.name));
                });
            }
        }
    });
}


$(document).ready(function () {
    GetProjectIdAndName();
    GetProjectTypeAndName();
});