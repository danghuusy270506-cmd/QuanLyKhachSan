// Hàm gọi AJAX chung để tải lại bảng dữ liệu
function loadNhanVienData(page, search) {
    $.ajax({
        url: '/NhanVien/Index',
        type: 'GET',
        data: { page: page, searchString: search },
        success: function (result) {
            $('#tableContainer').html(result);
        },
        error: function () {
            alert('Đã xảy ra lỗi khi tải dữ liệu!');
        }
    });
}

// Gắn các sự kiện khi trang đã tải xong
$(document).ready(function () {
    // Sự kiện Submit form tìm kiếm
    $(document).on('submit', '#searchForm', function (e) {
        e.preventDefault();
        var search = $('#searchString').val();
        loadNhanVienData(1, search);
    });

    // Sự kiện click nút làm mới (Reset)
    $(document).on('click', '#resetBtn', function () {
        $('#searchString').val('');
        loadNhanVienData(1, '');
    });

    // Sự kiện click chuyển trang trên phân trang AJAX
    $(document).on('click', '.ajax-link', function (e) {
        e.preventDefault();
        var page = $(this).data('page');
        var search = $('#searchString').val();
        if (page > 0) {
            loadNhanVienData(page, search);
        }
    });
});

// Sự kiện click nút Chi tiết để mở Modal qua AJAX
$(document).on('click', '.btn-detail', function (e) {
    e.preventDefault();
    var id = $(this).data('id');

    $.ajax({
        url: '/NhanVien/Details/' + id,
        type: 'GET',
        success: function (result) {
            // Đưa nội dung partial view vào bên trong modal content
            $('#detailModalContent').html(result);
            // Khởi tạo và hiển thị Bootstrap Modal
            var myModal = new bootstrap.Modal(document.getElementById('detailModal'));
            myModal.show();
        },
        error: function () {
            alert('Đã xảy ra lỗi khi tải thông tin chi tiết!');
        }
    });
});

// Sự kiện click nút Sửa để mở Modal qua AJAX
$(document).on('click', '.btn-edit', function (e) {
    e.preventDefault();
    var id = $(this).data('id');

    $.ajax({
        url: '/NhanVien/Edit/' + id,
        type: 'GET',
        success: function (result) {
            $('#editModalContent').html(result);
            var editModal = new bootstrap.Modal(document.getElementById('editModal'));
            editModal.show();
        },
        error: function () {
            alert('Đã xảy ra lỗi khi tải form chỉnh sửa!');
        }
    });
});

// Sự kiện click nút Lưu thay đổi trong Modal Sửa
$(document).on('click', '#saveEditBtn', function () {
    var form = $('#editNhanVienForm');
    var formData = form.serialize();
    var id = $('#MaNhanVien').val();

    $.ajax({
        url: '/NhanVien/Edit/' + id,
        type: 'POST',
        data: formData,
        success: function (response) {
            if (response.success) {
                // Đóng modal
                var editModalEl = document.getElementById('editModal');
                var modal = bootstrap.Modal.getInstance(editModalEl);
                if (modal) {
                    modal.hide();
                }

                // Tải lại bảng dữ liệu nhân viên (không hiển thị alert nữa)
                var currentPage = $('.pagination .active .page-link').text() || 1;
                var search = $('#searchString').val();
                loadNhanVienData(currentPage, search);
            } else {
                // Nếu dữ liệu không hợp lệ, tải lại partial view trong modal để hiển thị thông báo lỗi
                $('#editModalContent').html(response);
            }
        },
        error: function (xhr) {
            console.log(xhr.responseText);
            alert('Đã xảy ra lỗi khi cập nhật thông tin!');
        }
    });
});

// Sự kiện click nút Xóa để mở Modal qua AJAX (Không dùng confirm)
$(document).on('click', '.btn-delete', function (e) {
    e.preventDefault();
    var id = $(this).data('id');

    $.ajax({
        url: '/NhanVien/Delete/' + id,
        type: 'GET',
        success: function (result) {
            $('#deleteModalContent').html(result);
            var deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
            deleteModal.show();
        },
        error: function () {
            alert('Đã xảy ra lỗi khi tải form xác nhận xóa!');
        }
    });
});

// Sự kiện click nút "Đồng ý Xóa" bên trong Modal Xóa
$(document).on('click', '#confirmDeleteBtn', function () {
    var id = $('#MaNhanVienDelete').val();
    var token = $('#deleteNhanVienForm input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/NhanVien/Delete/' + id,
        type: 'POST',
        data: {
            __RequestVerificationToken: token
        },
        success: function (response) {
            if (response.success) {
                var deleteModalEl =
                    document.getElementById('deleteModal');

                var modal =
                    bootstrap.Modal.getInstance(deleteModalEl);

                if (modal) {
                    modal.hide();
                }

                // Hiển thị kết quả thực tế của thao tác
                if (response.message) {
                    alert(response.message);
                }

                // Tải lại bảng nhân viên
                var currentPage =
                    $('.pagination .active .page-link').text() || 1;

                var search = $('#searchString').val();

                loadNhanVienData(currentPage, search);
            } else {
                alert(
                    response.message ||
                    'Không thể xóa nhân viên này!'
                );
            }
        },
        error: function (xhr) {
            console.log(xhr.responseText);
            alert('Đã xảy ra lỗi khi xóa nhân viên!');
        }
    });
});