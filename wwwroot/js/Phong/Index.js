// Khai báo hàm loadPhongData ở phạm vi toàn cục
function loadPhongData(page = 1) {
    var soPhong = $('#soPhongInput').val();
    var tenLoaiPhong = $('#loaiPhongFilter').val();
    var trangThai = $('#trangThaiFilter').val();

    $.ajax({
        url: '/Phong/Index',
        type: 'GET',
        data: {
            soPhong: soPhong,
            tenLoaiPhong: tenLoaiPhong,
            trangThai: trangThai,
            page: page
        },
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (result) {
            $('#phongTableContainer').html(result);
        },
        error: function (xhr, status, error) {
            console.log("Error: " + error);
            alert('Có lỗi xảy ra khi tải dữ liệu!');
        }
    });
}

$(document).ready(function () {
    // Sự kiện bấm nút Lọc
    $('#btnFilter').click(function () {
        loadPhongData(1);
    });

    // Sự kiện bấm nút Xóa lọc (Reset)
    $('#btnReset').click(function () {
        $('#soPhongInput').val('');
        $('#loaiPhongFilter').val('Tất cả');
        $('#trangThaiFilter').val('Tất cả');
        loadPhongData(1);
    });

    // Sự kiện phân trang bằng Ajax
    $(document).on('click', '.custom-pagination .page-link', function (e) {
        e.preventDefault();
        var page = $(this).data('page');
        if (page) {
            loadPhongData(page);
        }
    });

    // Tìm kiếm ngay khi nhấn Enter ở ô số phòng
    $('#soPhongInput').keypress(function (e) {
        if (e.which == 13) {
            loadPhongData(1);
        }
    });
});

// Xử lý sự kiện bấm nút Xem chi tiết bằng AJAX
$(document).on('click', '.btn-view-details', function () {
    var id = $(this).data('id');
    $.get('/Phong/Details/' + id, function (data) {
        $('#detailModalContent').html(data);
        var detailModal = new bootstrap.Modal(document.getElementById('detailModal'));
        detailModal.show();
    }).fail(function () {
        alert('Không thể tải thông tin chi tiết phòng.');
    });
});

// Mở Modal Chỉnh Sửa bằng AJAX (GET)
$(document).on('click', '.btn-edit-phong', function (e) {
    e.preventDefault();
    var id = $(this).data('id');
    $.get('/Phong/Edit/' + id, function (data) {
        $('#editModalContent').html(data);
        var editModal = new bootstrap.Modal(document.getElementById('editModal'));
        editModal.show();
    }).fail(function () {
        alert('Không thể tải thông tin chỉnh sửa phòng.');
    });
});

// Bắt sự kiện Click nút "Lưu Thay Đổi" qua AJAX
$(document).off('click', '#btnSubmitEdit').on('click', '#btnSubmitEdit', function (e) {
    e.preventDefault();

    // Thu thập dữ liệu từ các input/select
    var postData = {
        MaPhong: $('#editMaPhong').val(),
        SoPhong: $('#editSoPhong').val(),
        Tang: $('#editTang').val(),
        MaLoaiPhong: $('#editMaLoaiPhong').val(),
        TrangThai: $('#editTrangThai').val(),
        __RequestVerificationToken: $('#editPhongFormContainer input[name="__RequestVerificationToken"]').val()
    };

    $.ajax({
        url: '/Phong/Edit/' + postData.MaPhong,
        type: 'POST',
        data: postData,
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (response) {
            if (response && response.success === true) {
                // Đóng Modal và dọn dẹp backdrop
                var editModalEl = document.getElementById('editModal');
                var modal = bootstrap.Modal.getInstance(editModalEl);
                if (modal) {
                    modal.hide();
                }

                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open').css({ 'overflow': '', 'padding-right': '' });

                // Lấy số trang hiện tại từ class .custom-pagination
                var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                loadPhongData(currentPage);
            } else {
                // Nếu server trả về partial view chứa thông báo lỗi, cập nhật lại nội dung modal mà không đóng modal
                $('#editModalContent').html(response);
            }
        },
        error: function () {
            alert('Có lỗi xảy ra khi lưu dữ liệu!');
        }
    });
});

// Mở Modal Xác Nhận Xóa bằng AJAX (GET)
$(document).on('click', '.btn-delete-phong', function (e) {
    e.preventDefault();
    var id = $(this).data('id');
    $.get('/Phong/Delete/' + id, function (data) {
        $('#deleteModalContent').html(data);
        var deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
        deleteModal.show();
    }).fail(function () {
        alert('Không thể tải thông tin xóa phòng.');
    });
});

// Bắt sự kiện Click nút "Xác Nhận Xóa" bằng AJAX (POST)
$(document).off('click', '#btnSubmitDelete').on('click', '#btnSubmitDelete', function (e) {
    e.preventDefault();

    var maPhong = $('#deleteMaPhong').val();
    var token = $('#deletePhongContainer input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Phong/Delete/' + maPhong,
        type: 'POST',
        data: {
            id: maPhong,
            __RequestVerificationToken: token
        },
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (response) {
            // Đóng Modal và dọn dẹp backdrop
            var deleteModalEl = document.getElementById('deleteModal');
            var modal = bootstrap.Modal.getInstance(deleteModalEl);
            if (modal) {
                modal.hide();
            }

            $('.modal-backdrop').remove();
            $('body').removeClass('modal-open').css({ 'overflow': '', 'padding-right': '' });

            if (response && response.success === true) {
                // Lấy số trang hiện tại để nạp lại danh sách
                var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                loadPhongData(currentPage);
            } else {
                alert('Xóa không thành công.');
            }
        },
        error: function () {
            $('.modal-backdrop').remove();
            $('body').removeClass('modal-open').css({ 'overflow': '', 'padding-right': '' });
            alert('Có lỗi xảy ra khi thực hiện xóa!');
        }
    });
});

// Mở Modal Thêm mới (GET)
$(document).on('click', '#btnOpenCreateModal', function (e) {
    e.preventDefault();
    $.get('/Phong/Create', function (data) {
        $('#createModalContent').html(data);
        var createModal = new bootstrap.Modal(document.getElementById('createModal'));
        createModal.show();
    }).fail(function () {
        alert('Không thể tải form thêm mới phòng.');
    });
});

// Bắt sự kiện Click nút "Lưu Phòng" (POST)
$(document).off('click', '#btnSubmitCreate').on('click', '#btnSubmitCreate', function (e) {
    e.preventDefault();

    var postData = {
        SoPhong: $('#createSoPhong').val(),
        Tang: $('#createTang').val(),
        MaLoaiPhong: $('#createMaLoaiPhong').val(),
        TrangThai: $('#createTrangThai').val(),
        __RequestVerificationToken: $('#createPhongFormContainer input[name="__RequestVerificationToken"]').val()
    };

    $.ajax({
        url: '/Phong/Create',
        type: 'POST',
        data: postData,
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (response) {
            if (response && response.success === true) {
                // Chỉ đóng Modal và dọn dẹp khi thành công
                var createModalEl = document.getElementById('createModal');
                var modal = bootstrap.Modal.getInstance(createModalEl);
                if (modal) {
                    modal.hide();
                }

                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open').css({ 'overflow': '', 'padding-right': '' });

                // Tải lại danh sách phòng ở trang hiện tại
                var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                loadPhongData(currentPage);
            } else {
                // Nếu có lỗi validation, GIỮ NGUYÊN MODAL và chỉ cập nhật lại nội dung form bên trong modal
                $('#createModalContent').html(response);
            }
        },
        error: function () {
            alert('Có lỗi xảy ra khi lưu dữ liệu!');
        }
    });
});