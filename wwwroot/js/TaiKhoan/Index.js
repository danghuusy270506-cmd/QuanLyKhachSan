document.addEventListener('DOMContentLoaded', function () {
    // ================================================================
    //  PHẦN SEARCH & LỌC AJAX (Dùng Form Lọc Chung)
    // ================================================================
    const searchForm = document.getElementById('searchForm');
    const tableContainer = document.getElementById('taiKhoanTableContainer');
    const btnClear = document.getElementById('btnClear'); // Nút làm mới chung

    // Hàm load dữ liệu tài khoản tổng quát
    window.loadTaiKhoanData = function (page = 1) {
        if (!searchForm || !tableContainer) return;

        // Lấy action URL từ form (VD: /TaiKhoan/Index)
        let baseUrl = searchForm.getAttribute('action') || '/TaiKhoan/Index';

        // Lấy toàn bộ dữ liệu từ các thẻ input/select trong form
        const formData = new FormData(searchForm);
        const params = new URLSearchParams(formData);

        // Thêm tham số page
        params.append('page', page);

        const url = `${baseUrl}?${params.toString()}`;

        // Hiệu ứng loading
        tableContainer.style.opacity = '0.5';

        fetch(url, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(response => {
                if (!response.ok) throw new Error('Lỗi khi tải dữ liệu');
                return response.text();
            })
            .then(html => {
                tableContainer.innerHTML = html;
                tableContainer.style.opacity = '1';

                // Cập nhật URL trên thanh địa chỉ trình duyệt (Không bắt buộc)
                window.history.pushState({}, "", url);
            })
            .catch(error => {
                console.error('Lỗi khi tìm kiếm AJAX:', error);
                tableContainer.style.opacity = '1';
                alert('Có lỗi xảy ra khi tải dữ liệu!');
            });
    };

    // Bắt sự kiện Submit Form (Bấm nút Lọc hoặc Enter)
    if (searchForm) {
        searchForm.addEventListener('submit', function (e) {
            e.preventDefault();
            loadTaiKhoanData(1);
        });
    }

    // Bắt sự kiện bấm nút Làm mới (Xóa lọc)
    if (btnClear) {
        btnClear.addEventListener('click', function (e) {
            e.preventDefault();
            searchForm.reset(); // Xóa trắng tất cả input/select
            loadTaiKhoanData(1);
        });
    }

    // Bắt sự kiện Phân trang AJAX (class ajax-link)
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.ajax-link');
        if (!btn) return;
        e.preventDefault();

        const page = btn.dataset.page;
        if (page && page > 0) {
            loadTaiKhoanData(page);
        }
    });

    // ================================================================
    //  MODAL CHI TIẾT
    // ================================================================
    $(document).on('click', '.btn-view-detail', function () {
        var id = $(this).data('id');

        $('#detailModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status"></div>
            </div>
        `);

        var myModal = new bootstrap.Modal(document.getElementById('detailModal'));
        myModal.show();

        $.ajax({
            url: '/TaiKhoan/Details',
            type: 'GET',
            data: { mataikhoan: id },
            success: function (result) {
                $('#detailModalBody').html(result);
            },
            error: function (xhr) {
                if (xhr.status === 403) {
                    $('#detailModalBody').html('<div class="alert alert-danger m-3">Bạn không có quyền xem chi tiết tài khoản này!</div>');
                } else {
                    $('#detailModalBody').html('<div class="alert alert-danger m-3">Không thể tải dữ liệu chi tiết.</div>');
                }
            }
        });
    });

    // ================================================================
    //  MODAL SỬA
    // ================================================================
    $(document).on('click', '.btn-edit-modal', function () {
        var id = $(this).data('id');

        $('#editModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-warning" role="status"></div>
            </div>
        `);

        var editModal = new bootstrap.Modal(document.getElementById('editModal'));
        editModal.show();

        $.ajax({
            url: '/TaiKhoan/Edit',
            type: 'GET',
            data: { mataikhoan: id },
            success: function (result) {
                $('#editModalBody').html(result);
            },
            error: function () {
                $('#editModalBody').html('<div class="alert alert-danger m-3">Không thể tải dữ liệu form sửa.</div>');
            }
        });
    });

    // Submit Sửa
    $(document).on('click', '#btnSaveEdit', function () {
        var $form = $('#editTaiKhoanForm');
        if ($form.length === 0) return;

        var formData = $form.serialize();

        $.ajax({
            url: $form.attr('action') || '/TaiKhoan/Edit',
            type: 'POST',
            data: formData,
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    var modalEl = document.getElementById('editModal');
                    var modal = bootstrap.Modal.getInstance(modalEl);
                    if (modal) modal.hide();

                    // Tải lại bảng dữ liệu ngầm mà không hiển thị thông báo alert
                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    loadTaiKhoanData(currentPage);
                } else {
                    $('#editModalBody').html(response);
                }
            },
            error: function () {
                alert("Đã xảy ra lỗi trong quá trình lưu dữ liệu!");
            }
        });
    });

    // ================================================================
    //  MODAL XÓA
    // ================================================================
    $(document).on('click', '.btn-delete-modal', function () {
        var id = $(this).data('id');

        $('#deleteModalBody').html(`
            <div class="text-center py-4">
                <div class="spinner-border text-danger" role="status"></div>
            </div>
        `);

        var deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
        deleteModal.show();

        $.ajax({
            url: '/TaiKhoan/Delete',
            type: 'GET',
            data: { mataikhoan: id },
            success: function (result) {
                $('#deleteModalBody').html(result);
            },
            error: function () {
                $('#deleteModalBody').html('<div class="alert alert-danger m-3">Không thể tải dữ liệu xóa.</div>');
            }
        });
    });

    // Submit Xóa
    $(document).on('click', '#btnConfirmDelete', function () {
        var $form = $('#deleteTaiKhoanForm');
        if ($form.length === 0) return;

        var formData = $form.serialize();

        $.ajax({
            url: $form.attr('action') || '/TaiKhoan/Delete',
            type: 'POST',
            data: formData,
            success: function (response) {
                if (typeof response === 'object' && response.success) {
                    var modalEl = document.getElementById('deleteModal');
                    var modal = bootstrap.Modal.getInstance(modalEl);
                    if (modal) modal.hide();

                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    loadTaiKhoanData(currentPage);
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