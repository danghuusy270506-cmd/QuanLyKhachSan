document.addEventListener('DOMContentLoaded', function () {
    // ================================================================
    //  PHẦN SEARCH & LỌC AJAX (Dùng Form Lọc Chung)
    // ================================================================
    const searchForm = document.getElementById('searchForm');
    const tableContainer = document.getElementById('khachHangContainer');
    const inputTuKhoa = document.getElementById('tuKhoa'); // Input trong form chung
    const btnClear = document.getElementById('btnClear');   // Nút làm mới chung

    // Hàm load dữ liệu khách hàng tổng quát
    window.loadKhachHangData = function (page = 1) {
        if (!searchForm || !tableContainer) return;

        // Lấy action URL từ form
        let baseUrl = searchForm.getAttribute('action') || '/KhachHang/Index';

        // Tự động lấy toàn bộ dữ liệu từ các thẻ input/select trong form
        const formData = new FormData(searchForm);
        const params = new URLSearchParams(formData);

        // Thêm tham số page
        params.append('page', page);

        const url = `${baseUrl}?${params.toString()}`;

        // Hiệu ứng loading
        tableContainer.style.opacity = '0.5';

        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(response => {
                if (!response.ok) throw new Error('Lỗi khi tải dữ liệu');
                return response.text();
            })
            .then(html => {
                tableContainer.innerHTML = html;
                tableContainer.style.opacity = '1';

                // Re-bind Client-side Validation nếu có dùng jQuery
                if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
                    jQuery.validator.unobtrusive.parse(tableContainer);
                }
            })
            .catch(error => {
                console.error('Lỗi khi tìm kiếm AJAX:', error);
                tableContainer.style.opacity = '1';
            });
    };

    // Bắt sự kiện Submit Form (Bấm nút Lọc hoặc Enter)
    if (searchForm) {
        searchForm.addEventListener('submit', function (e) {
            e.preventDefault();
            loadKhachHangData(1);
        });
    }

    // Bắt sự kiện bấm nút Làm mới (Xóa lọc)
    if (btnClear) {
        btnClear.addEventListener('click', function (e) {
            e.preventDefault();
            searchForm.reset(); // Xóa trắng tất cả input/select trong form
            loadKhachHangData(1);
        });
    }

    // Bắt sự kiện Phân trang AJAX (class ajax-page-link)
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.ajax-page-link');
        if (!btn) return;
        e.preventDefault();

        const page = btn.dataset.page;
        if (page && page > 0) {
            loadKhachHangData(page);
        }
    });

    // ================================================================
    //  MODAL THÊM MỚI
    // ================================================================
    $(document).on('click', '#btnOpenCreateModal', function (e) {
        e.preventDefault();
        $('#createKhachHangModal').modal('show');
        $('#createModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status"></div>
                <p class="mt-2 text-muted small">Đang tải form nhập liệu...</p>
            </div>
        `);

        $.ajax({
            url: '/KhachHang/Create',
            type: 'GET',
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                $('#createModalBody').html(response);
                if ($.validator && $.validator.unobtrusive) {
                    $.validator.unobtrusive.parse("#formCreateKhachHang");
                }
            },
            error: function () {
                $('#createModalBody').html(`<div class="alert alert-danger mb-0">Không thể tải form thêm mới.</div>`);
            }
        });
    });

    // Submit Form Thêm Mới
    $(document).on('click', '#btnSubmitKhachHang', function (e) {
        e.preventDefault();
        var $form = $('#formCreateKhachHang');
        var $submitBtn = $(this);
        var originalBtnHtml = $submitBtn.html();

        if ($form.valid && !$form.valid()) return false;
        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    $('#createKhachHangModal').modal('hide');
                    loadKhachHangData(1);
                } else {
                    $('#createModalBody').html(response);
                    if ($.validator && $.validator.unobtrusive) {
                        $.validator.unobtrusive.parse("#formCreateKhachHang");
                    }
                }
            },
            error: function () { alert("Đã xảy ra lỗi hệ thống khi lưu thông tin!"); },
            complete: function () { $submitBtn.prop('disabled', false).html(originalBtnHtml); }
        });
    });

    // ================================================================
    //  MODAL CHI TIẾT
    // ================================================================
    $(document).on('click', '.btn-view-details', function (e) {
        e.preventDefault();
        var maKhachHang = $(this).data('id');
        $('#detailsKhachHangModal').modal('show');
        $('#detailsModalBody').html(`<div class="text-center py-4"><div class="spinner-border text-info" role="status"></div></div>`);

        $.ajax({
            url: '/KhachHang/Details',
            type: 'GET',
            data: { maKhachHang: maKhachHang },
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) { $('#detailsModalBody').html(response); },
            error: function () { $('#detailsModalBody').html(`<div class="alert alert-danger">Không thể tải chi tiết.</div>`); }
        });
    });

    // ================================================================
    //  MODAL SỬA
    // ================================================================
    $(document).on('click', '.btn-open-edit-modal', function (e) {
        e.preventDefault();
        var maKhachHang = $(this).data('id');
        $('#editKhachHangModal').modal('show');
        $('#editModalBody').html(`<div class="text-center py-4"><div class="spinner-border text-warning" role="status"></div></div>`);

        $.ajax({
            url: '/KhachHang/Edit',
            type: 'GET',
            data: { maKhachHang: maKhachHang },
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                $('#editModalBody').html(response);
                if ($.validator && $.validator.unobtrusive) {
                    $.validator.unobtrusive.parse("#editKhachHangForm");
                }
            },
            error: function () { $('#editModalBody').html(`<div class="alert alert-danger">Không thể tải form sửa.</div>`); }
        });
    });

    // Submit Form Sửa
    $(document).on('click', '#btnSubmitEditKhachHang', function (e) {
        e.preventDefault();
        var $form = $('#editKhachHangForm');
        var $submitBtn = $(this);
        var originalBtnHtml = $submitBtn.html();

        if ($form.valid && !$form.valid()) return false;
        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize(),
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    $('#editKhachHangModal').modal('hide');
                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    loadKhachHangData(currentPage);
                } else {
                    $('#editModalBody').html(response);
                    if ($.validator && $.validator.unobtrusive) {
                        $.validator.unobtrusive.parse("#editKhachHangForm");
                    }
                }
            },
            error: function () { alert("Đã xảy ra lỗi hệ thống khi cập nhật!"); },
            complete: function () { $submitBtn.prop('disabled', false).html(originalBtnHtml); }
        });
    });

    // ================================================================
    //  MODAL XÓA
    // ================================================================
    $(document).on('click', '.btn-open-delete-modal', function (e) {
        e.preventDefault();
        var maKhachHang = $(this).data('id');
        $('#deleteKhachHangModal').modal('show');
        $('#deleteModalBody').html(`<div class="text-center py-4"><div class="spinner-border text-danger" role="status"></div></div>`);

        $.ajax({
            url: '/KhachHang/Delete',
            type: 'GET',
            data: { maKhachHang: maKhachHang },
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) { $('#deleteModalBody').html(response); },
            error: function () { $('#deleteModalBody').html(`<div class="alert alert-danger">Không thể tải form xóa.</div>`); }
        });
    });

    // Submit Form Xóa
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
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    $('#deleteKhachHangModal').modal('hide');
                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    loadKhachHangData(currentPage);
                } else {
                    alert(response.message || "Không thể xóa khách hàng này.");
                }
            },
            error: function () { alert("Đã xảy ra lỗi hệ thống khi xóa!"); },
            complete: function () { $submitBtn.prop('disabled', false).html(originalBtnHtml); }
        });
    });
});