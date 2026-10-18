$(document).ready(function () {

    // === HÀM DÙNG CHUNG ĐỂ TẢI DỮ LIỆU KHÁCH HÀNG QUA AJAX (PHÂN TRANG & TÌM KIẾM) ===
    window.loadKhachHangData = function (page, tuKhoa) {
        $.ajax({
            url: '/KhachHang/Index',
            type: 'GET',
            data: {
                page: page,
                tuKhoa: tuKhoa
            },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                // Đưa HTML trả về vào vùng chứa bảng
                $('#khachHangContainer').html(response);
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi tải dữ liệu phân trang:", error);
            }
        });
    };

    // 1. Mở Modal và tải Form qua Ajax
    $(document).on('click', '#btnOpenCreateModal', function (e) {
        e.preventDefault();

        // Hiển thị modal và spinner loading
        $('#createKhachHangModal').modal('show');
        $('#createModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
                <p class="mt-2 text-muted small">Đang tải form nhập liệu...</p>
            </div>
        `);

        // Gọi Ajax lấy PartialView Form
        $.ajax({
            url: '/KhachHang/Create',
            type: 'GET',
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                $('#createModalBody').html(response);

                // Re-bind Client-side Validation (jQuery Unobtrusive Validation)
                if ($.validator && $.validator.unobtrusive) {
                    $.validator.unobtrusive.parse("#formCreateKhachHang");
                }
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi tải form:", error);
                $('#createModalBody').html(`
                    <div class="alert alert-danger mb-0">
                        <i class="bi bi-exclamation-triangle-fill me-2"></i> Không thể tải form thêm mới. Vui lòng thử lại sau!
                    </div>
                `);
            }
        });
    });

    // 2. Xử lý sự kiện click vào nút Lưu (Thêm mới khách hàng)
    $(document).on('click', '#btnSubmitKhachHang', function (e) {
        e.preventDefault();

        var $form = $('#formCreateKhachHang');
        var $submitBtn = $(this);
        var originalBtnHtml = $submitBtn.html();

        if ($form.valid && !$form.valid()) {
            return false;
        }

        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize(),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    $('#createKhachHangModal').modal('hide');
                    $form[0].reset();

                    // Tải lại danh sách ở trang 1 kèm từ khóa hiện tại (nếu đang tìm kiếm)
                    var tuKhoa = $('#tuKhoaInput').val() || '';
                    loadKhachHangData(1, tuKhoa);
                } else {
                    $('#createModalBody').html(response);
                    if ($.validator && $.validator.unobtrusive) {
                        $.validator.unobtrusive.parse("#formCreateKhachHang");
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi lưu dữ liệu:", error);
                alert("Đã xảy ra lỗi hệ thống khi lưu thông tin khách hàng!");
            },
            complete: function () {
                $submitBtn.prop('disabled', false).html(originalBtnHtml);
            }
        });
    });

    // 3. Xử lý sự kiện click chuyển trang (Phân trang AJAX)
    $(document).on('click', '.ajax-page-link', function (e) {
        e.preventDefault();

        var page = $(this).data('page');
        var tuKhoa = $('#tuKhoaInput').val() || '';

        if (page && page > 0) {
            loadKhachHangData(page, tuKhoa);
        }
    });

    // 4. Xử lý sự kiện submit Form tìm kiếm bằng AJAX
    $(document).on('submit', '#searchForm', function (e) {
        e.preventDefault();

        var tuKhoa = $('#tuKhoaInput').val() || '';
        loadKhachHangData(1, tuKhoa); // Tìm kiếm luôn về trang 1
    });

    // 5. Xử lý nút Reset bộ lọc
    $(document).on('click', '#btnReset', function () {
        $('#tuKhoaInput').val('');
        loadKhachHangData(1, '');
    });

    // 6. Mở Modal và tải Chi tiết khách hàng qua Ajax
    $(document).on('click', '.btn-view-details', function (e) {
        e.preventDefault();

        var maKhachHang = $(this).data('id');

        // Hiển thị modal và spinner loading
        $('#detailsKhachHangModal').modal('show');
        $('#detailsModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-info" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
                <p class="mt-2 text-muted small">Đang tải thông tin chi tiết...</p>
            </div>
        `);

        // Gọi Ajax lấy PartialView Details
        $.ajax({
            url: '/KhachHang/Details',
            type: 'GET',
            data: { maKhachHang: maKhachHang },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                $('#detailsModalBody').html(response);
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi tải chi tiết:", error);
                $('#detailsModalBody').html(`
                    <div class="alert alert-danger mb-0">
                        <i class="bi bi-exclamation-triangle-fill me-2"></i> Không thể tải thông tin chi tiết. Vui lòng thử lại sau!
                    </div>
                `);
            }
        });
    });

    // 7. Mở Modal và tải Form Sửa khách hàng qua Ajax
    $(document).on('click', '.btn-open-edit-modal', function (e) {
        e.preventDefault();
        var maKhachHang = $(this).data('id');

        // Hiển thị modal và trạng thái đang tải (Loading)
        $('#editKhachHangModal').modal('show');
        $('#editModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-warning" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
                <p class="mt-2 text-muted small">Đang tải thông tin chỉnh sửa...</p>
            </div>
        `);

        // Gọi Ajax lấy PartialView Form Sửa
        $.ajax({
            url: '/KhachHang/Edit',
            type: 'GET',
            data: { maKhachHang: maKhachHang },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                $('#editModalBody').html(response);

                // Re-bind Client-side Validation nếu có
                if ($.validator && $.validator.unobtrusive) {
                    $.validator.unobtrusive.parse("#editKhachHangForm");
                }
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi tải form sửa:", error);
                $('#editModalBody').html(`
                    <div class="alert alert-danger mb-0">
                        <i class="bi bi-exclamation-triangle-fill me-2"></i> Không thể tải form chỉnh sửa. Vui lòng thử lại sau!
                    </div>
                `);
            }
        });
    });

    // 8. Xử lý sự kiện click nút Lưu thay đổi trong Modal Sửa
    $(document).on('click', '#btnSubmitEditKhachHang', function (e) {
        e.preventDefault();

        var $form = $('#editKhachHangForm');
        var $submitBtn = $(this);
        var originalBtnHtml = $submitBtn.html();

        if ($form.valid && !$form.valid()) {
            return false;
        }

        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize(),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    // Đóng modal, thông báo thành công (hoặc toast tùy ý) và load lại bảng
                    $('#editKhachHangModal').modal('hide');

                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    var tuKhoa = $('#tuKhoaInput').val() || '';
                    loadKhachHangData(currentPage, tuKhoa);
                } else {
                    // Nếu validation lỗi từ server trả về lại PartialView kèm lỗi
                    $('#editModalBody').html(response);
                    if ($.validator && $.validator.unobtrusive) {
                        $.validator.unobtrusive.parse("#editKhachHangForm");
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi cập nhật dữ liệu:", error);
                alert("Đã xảy ra lỗi hệ thống khi cập nhật thông tin!");
            },
            complete: function () {
                $submitBtn.prop('disabled', false).html(originalBtnHtml);
            }
        });
    });

    // 9. Mở Modal và tải Form Xóa khách hàng qua Ajax (Không dùng confirm truyền thống)
    $(document).on('click', '.btn-open-delete-modal', function (e) {
        e.preventDefault();
        var maKhachHang = $(this).data('id');

        // Hiển thị modal và trạng thái đang tải (Loading)
        $('#deleteKhachHangModal').modal('show');
        $('#deleteModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-danger" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
                <p class="mt-2 text-muted small">Đang tải thông tin xác nhận xóa...</p>
            </div>
        `);

        // Gọi Ajax lấy PartialView Form Xóa
        $.ajax({
            url: '/KhachHang/Delete',
            type: 'GET',
            data: { maKhachHang: maKhachHang },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                $('#deleteModalBody').html(response);
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi tải form xóa:", error);
                $('#deleteModalBody').html(`
                    <div class="alert alert-danger mb-0">
                        <i class="bi bi-exclamation-triangle-fill me-2"></i> Không thể tải thông tin xóa. Vui lòng thử lại sau!
                    </div>
                `);
            }
        });
    });

    // 10. Xử lý sự kiện click nút Xác nhận xóa trong Modal Xóa
    $(document).on('click', '#btnSubmitDeleteKhachHang', function (e) {
        e.preventDefault();

        var $form = $('#formDeleteKhachHang');
        var $submitBtn = $(this);
        var originalBtnHtml = $submitBtn.html();

        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang xóa...');

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize(),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    // Đóng modal và tải lại bảng dữ liệu ở trang hiện tại
                    $('#deleteKhachHangModal').modal('hide');

                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    var tuKhoa = $('#tuKhoaInput').val() || '';
                    loadKhachHangData(currentPage, tuKhoa);
                } else {
                    alert(response.message || "Không thể xóa khách hàng này.");
                }
            },
            error: function (xhr, status, error) {
                console.error("Lỗi khi xóa dữ liệu:", error);
                alert("Đã xảy ra lỗi hệ thống khi xóa thông tin!");
            },
            complete: function () {
                $submitBtn.prop('disabled', false).html(originalBtnHtml);
            }
        });
    });
});