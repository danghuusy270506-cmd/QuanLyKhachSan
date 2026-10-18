// Hàm gọi AJAX tải dữ liệu danh sách Loại Phòng theo trang
function loadLoaiPhongData(page = 1) {
    $.ajax({
        url: '/LoaiPhong/Index',
        type: 'GET',
        data: { page: page },
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (result) {
            $('#loaiPhongTableContainer').html(result);
        },
        error: function (xhr, status, error) {
            console.log("Error: " + error);
            alert('Có lỗi xảy ra khi tải dữ liệu loại phòng!');
        }
    });
}

// Hàm helper để quản lý và tái sử dụng Bootstrap Modal instance
function getLoaiPhongModal() {
    var modalEl = document.getElementById('loaiPhongModal');
    var modal = bootstrap.Modal.getInstance(modalEl);
    if (!modal) {
        modal = new bootstrap.Modal(modalEl, {
            backdrop: true, // Cho phép click ra ngoài để đóng modal
            keyboard: true  // Cho phép bấm nút ESC để đóng modal
        });
    }
    return modal;
}

// Chi tiết
$(document).ready(function () {
    // Sự kiện click mở Modal xem chi tiết loại phòng
    $(document).on('click', '.btn-detail', function (e) {
        e.preventDefault();
        var id = $(this).data('id');

        if (id) {
            $.ajax({
                url: '/LoaiPhong/Details',
                type: 'GET',
                data: { maloaiphong: id },
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                success: function (result) {
                    $('#modalContentContainer').html(result);
                    var detailModal = getLoaiPhongModal();
                    detailModal.show();
                },
                error: function (xhr, status, error) {
                    console.log("Error: " + error);
                    alert('Không thể tải thông tin chi tiết loại phòng!');
                }
            });
        }
    });
});

// Sửa
$(document).on('click', '.btn-edit', function (e) {
    e.preventDefault();
    var id = $(this).data('id');

    if (id) {
        $.ajax({
            url: '/LoaiPhong/Edit',
            type: 'GET',
            data: { maloaiphong: id },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (result) {
                $('#modalContentContainer').html(result);
                var editModal = getLoaiPhongModal();
                editModal.show();
            },
            error: function (xhr, status, error) {
                console.log("Error: " + error);
                alert('Không thể tải form chỉnh sửa loại phòng!');
            }
        });
    }
});

// Submit sửa
$(document).on('submit', '#editLoaiPhongForm', function (e) {
    e.preventDefault();
    var form = $(this);
    var formData = new FormData(this);

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (response) {
            if (response.success) {
                // Đóng modal an toàn bằng instance hiện có
                var modalEl = document.getElementById('loaiPhongModal');
                var modalInstance = bootstrap.Modal.getInstance(modalEl);
                if (modalInstance) {
                    modalInstance.hide();
                }

                // Tải lại bảng dữ liệu danh sách mà không cần reload trang
                loadLoaiPhongData();
            } else {
                $('#modalContentContainer').html(response);
            }
        },
        error: function (xhr, status, error) {
            console.log("Error: " + error);
            alert('Có lỗi xảy ra khi lưu thay đổi!');
        }
    });
});

// Xóa
$(document).on('click', '.btn-delete', function (e) {
    e.preventDefault();
    var id = $(this).data('id');

    if (id) {
        $.ajax({
            url: '/LoaiPhong/Delete',
            type: 'GET',
            data: { maloaiphong: id },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (result) {
                $('#modalContentContainer').html(result);
                var deleteModal = getLoaiPhongModal();
                deleteModal.show();
            },
            error: function (xhr, status, error) {
                console.log("Error: " + error);
                alert('Không thể tải thông tin xóa loại phòng!');
            }
        });
    }
});

// Submit xóa
$(document).on('submit', '#deleteLoaiPhongForm', function (e) {
    e.preventDefault();
    var form = $(this);

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (response) {
            if (response.success) {
                var modalEl = document.getElementById('loaiPhongModal');
                var modalInstance = bootstrap.Modal.getInstance(modalEl);
                if (modalInstance) {
                    modalInstance.hide();
                }

                loadLoaiPhongData();
            } else {
                alert('Có lỗi xảy ra khi xóa!');
            }
        },
        error: function (xhr, status, error) {
            console.log("Error: " + error);
            alert('Có lỗi xảy ra khi thực hiện xóa!');
        }
    });
});

// Thêm
$(document).on('click', '#btnOpenCreateModal', function (e) {
    e.preventDefault();
    $.ajax({
        url: '/LoaiPhong/Create',
        type: 'GET',
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (result) {
            $('#modalContentContainer').html(result);
            var createModal = getLoaiPhongModal();
            createModal.show();
        },
        error: function (xhr, status, error) {
            console.log("Error: " + error);
            alert('Không thể mở form thêm mới loại phòng!');
        }
    });
});

// Submit thêm
$(document).on('submit', '#createLoaiPhongForm', function (e) {
    e.preventDefault();
    var form = $(this);
    var formData = new FormData(this);

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (response) {
            if (response.success) {
                var modalEl = document.getElementById('loaiPhongModal');
                var modalInstance = bootstrap.Modal.getInstance(modalEl);
                if (modalInstance) {
                    modalInstance.hide();
                }

                loadLoaiPhongData();
            } else {
                $('#modalContentContainer').html(response);
            }
        },
        error: function (xhr, status, error) {
            console.log("Error: " + error);
            alert('Có lỗi xảy ra khi thêm mới loại phòng!');
        }
    });
});