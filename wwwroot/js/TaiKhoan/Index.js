$(document).ready(function () {
    // Hàm chung để gọi AJAX tải lại bảng dữ liệu (hỗ trợ cả phân trang và lọc)
    function loadTaiKhoanData(page, keyword, role) {
        $.ajax({
            url: '/TaiKhoan/Index',
            type: 'GET',
            data: {
                page: page,
                keyword: keyword,
                role: role
            },
            success: function (result) {
                // Thay thế nội dung cũ bằng partial view trả về từ server
                $('#taiKhoanTableContainer').html(result);

                // Cập nhật lại URL trên thanh địa chỉ trình duyệt
                var queryParams = "?page=" + page;
                if (keyword) queryParams += "&keyword=" + encodeURIComponent(keyword);
                if (role) queryParams += "&role=" + encodeURIComponent(role);

                window.history.pushState({ page: page, keyword: keyword, role: role }, "", "/TaiKhoan/Index" + queryParams);
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi tải dữ liệu:", error);
                alert("Không thể tải dữ liệu. Vui lòng thử lại!");
            }
        });
    }

    // Bắt sự kiện click vào các nút phân trang (Ủy quyền sự kiện)
    $(document).on('click', '.ajax-link', function (e) {
        e.preventDefault();

        var page = $(this).data('page');
        if (!page || page < 1) return;

        var keyword = $('#searchKeyword').val();
        var role = $('#filterRole').val();

        loadTaiKhoanData(page, keyword, role);
    });

    // Bắt sự kiện click nút "Lọc"
    $('#btnFilter').click(function () {
        var keyword = $('#searchKeyword').val();
        var role = $('#filterRole').val();
        loadTaiKhoanData(1, keyword, role);
    });

    // Cho phép nhấn Enter ở ô tìm kiếm để lọc luôn
    $('#searchKeyword').keypress(function (e) {
        if (e.which === 13) {
            $('#btnFilter').click();
            return false;
        }
    });

    // Bắt sự kiện click nút "Reset" (Làm mới bộ lọc)
    $('#btnReset').click(function () {
        $('#searchKeyword').val('');
        $('#filterRole').val('');
        loadTaiKhoanData(1, '', '');
    });

    $(document).ready(function () {
        // ... (các đoạn code cũ giữ nguyên)

        // 5. Bắt sự kiện click nút "Chi tiết" để mở Modal bằng AJAX
        $(document).on('click', '.btn-view-detail', function () {
            var id = $(this).data('id');

            // Hiển thị trạng thái đang tải trước khi AJAX trả về kết quả
            $('#detailModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            </div>
        `);

            // Mở Modal Bootstrap
            var myModal = new bootstrap.Modal(document.getElementById('detailModal'));
            myModal.show();

            // Gọi AJAX lấy nội dung chi tiết
            $.ajax({
                url: '/TaiKhoan/Details',
                type: 'GET',
                data: { mataikhoan: id },
                success: function (result) {
                    $('#detailModalBody').html(result);
                },
                error: function (xhr) {
                    if (xhr.status === 403) {
                        $('#detailModalBody').html('<div class="alert alert-danger">Bạn không có quyền xem chi tiết tài khoản này!</div>');
                    } else {
                        $('#detailModalBody').html('<div class="alert alert-danger">Không thể tải dữ liệu chi tiết. Vui lòng thử lại sau!</div>');
                    }
                }
            });
        });
    });

    // Bắt sự kiện click nút "Sửa" để mở Modal bằng AJAX
    $(document).on('click', '.btn-edit-modal', function () {
        var id = $(this).data('id');

        // Hiển thị loading trước khi AJAX trả về
        $('#editModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-warning" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            </div>
        `);

        // Mở Modal Bootstrap
        var editModal = new bootstrap.Modal(document.getElementById('editModal'));
        editModal.show();

        // Gọi AJAX lấy nội dung form Edit
        $.ajax({
            url: '/TaiKhoan/Edit',
            type: 'GET',
            data: { mataikhoan: id },
            success: function (result) {
                $('#editModalBody').html(result);
            },
            error: function () {
                $('#editModalBody').html('<div class="alert alert-danger">Không thể tải dữ liệu form sửa. Vui lòng thử lại!</div>');
            }
        });
    });

    // Xử lý sự kiện bấm nút "Lưu thay đổi" trong Modal Sửa
    $('#btnSaveEdit').click(function () {
        var $form = $('#editTaiKhoanForm');
        if ($form.length === 0) return;

        var formData = $form.serialize();

        $.ajax({
            url: $form.attr('action') || '/TaiKhoan/Edit',
            type: 'POST',
            data: formData,
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    // 1. Đóng modal
                    var modalEl = document.getElementById('editModal');
                    var modal = bootstrap.Modal.getInstance(modalEl);
                    modal.hide();

                    // 2. Tải lại bảng dữ liệu ngầm mà không hiển thị thông báo alert
                    var currentPage = $('.pagination .active .ajax-link').data('page') || 1;
                    var keyword = $('#searchKeyword').val();
                    var role = $('#filterRole').val();
                    loadTaiKhoanData(currentPage, keyword, role);

                    // Đã bỏ dòng alert(response.message); tại đây
                } else {
                    // Nếu validation lỗi, server trả về lại partial view chứa lỗi
                    $('#editModalBody').html(response);
                }
            },
            error: function () {
                alert("Đã xảy ra lỗi trong quá trình lưu dữ liệu!");
            }
        });
    });

    //Bắt sự kiện click nút "Xóa" trên bảng để mở Modal bằng AJAX
    $(document).on('click', '.btn-delete-modal', function () {
        var id = $(this).data('id');

        // Hiển thị trạng thái đang tải
        $('#deleteModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-danger" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            </div>
        `);

        // Mở Modal Bootstrap
        var deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
        deleteModal.show();

        // Gọi AJAX lấy nội dung xác nhận xóa
        $.ajax({
            url: '/TaiKhoan/Delete',
            type: 'GET',
            data: { mataikhoan: id },
            success: function (result) {
                $('#deleteModalBody').html(result);
            },
            error: function () {
                $('#deleteModalBody').html('<div class="alert alert-danger">Không thể tải dữ liệu xóa. Vui lòng thử lại!</div>');
            }
        });
    });

    //Xử lý sự kiện bấm nút "Xác Nhận Xóa" trong Modal
    $('#btnConfirmDelete').click(function () {
        var $form = $('#deleteTaiKhoanForm');
        if ($form.length === 0) return;

        var formData = $form.serialize();

        $.ajax({
            url: $form.attr('action') || '/TaiKhoan/Delete',
            type: 'POST',
            data: formData,
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    // Đóng modal
                    var modalEl = document.getElementById('deleteModal');
                    var modal = bootstrap.Modal.getInstance(modalEl);
                    modal.hide();

                    // Tải lại bảng dữ liệu ngầm trang hiện tại
                    var currentPage = $('.pagination .active .ajax-link').data('page') || 1;
                    var keyword = $('#searchKeyword').val();
                    var role = $('#filterRole').val();
                    loadTaiKhoanData(currentPage, keyword, role);
                } else {
                    alert(response.message || "Đã xảy ra lỗi khi xóa tài khoản!");
                }
            },
            error: function () {
                alert("Đã xảy ra lỗi trong quá trình kết nối với máy chủ!");
            }
        });
    });
});