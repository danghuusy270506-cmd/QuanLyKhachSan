document.addEventListener('DOMContentLoaded', function () {
    // ================================================================
    //  PHẦN SEARCH & LỌC AJAX (Dùng Form Lọc Chung)
    // ================================================================
    const searchForm = document.getElementById('searchForm');
    const tableContainer = document.getElementById('tableContainer'); // Hoặc khachHangContainer... tùy ID bọc bảng của bạn
    const btnClear = document.getElementById('btnClear'); // Nút làm mới chung

    // Hàm load dữ liệu nhân viên tổng quát
    window.loadNhanVienData = function (page = 1) {
        if (!searchForm || !tableContainer) return;

        // Lấy action URL từ form (VD: /NhanVien/Index)
        let baseUrl = searchForm.getAttribute('action') || '/NhanVien/Index';

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
            loadNhanVienData(1);
        });
    }

    // Bắt sự kiện bấm nút Làm mới (Xóa lọc)
    if (btnClear) {
        btnClear.addEventListener('click', function (e) {
            e.preventDefault();
            searchForm.reset(); // Xóa trắng tất cả input trong form
            loadNhanVienData(1);
        });
    }

    // Bắt sự kiện Phân trang AJAX (class ajax-link)
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.ajax-link');
        if (!btn) return;
        e.preventDefault();

        const page = btn.dataset.page;
        if (page && page > 0) {
            loadNhanVienData(page);
        }
    });

    // ================================================================
    //  MODAL CHI TIẾT
    // ================================================================
    $(document).on('click', '.btn-detail', function (e) {
        e.preventDefault();
        var id = $(this).data('id');

        $.ajax({
            url: '/NhanVien/Details/' + id,
            type: 'GET',
            success: function (result) {
                $('#detailModalContent').html(result);
                var myModal = new bootstrap.Modal(document.getElementById('detailModal'));
                myModal.show();
            },
            error: function () {
                alert('Đã xảy ra lỗi khi tải thông tin chi tiết!');
            }
        });
    });

    // ================================================================
    //  MODAL SỬA
    // ================================================================
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

    // Submit Sửa
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
                    var editModalEl = document.getElementById('editModal');
                    var modal = bootstrap.Modal.getInstance(editModalEl);
                    if (modal) {
                        modal.hide();
                    }

                    // Tính toán số trang hiện tại dựa vào phân trang
                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    loadNhanVienData(currentPage);
                } else {
                    $('#editModalContent').html(response);
                }
            },
            error: function (xhr) {
                console.log(xhr.responseText);
                alert('Đã xảy ra lỗi khi cập nhật thông tin!');
            }
        });
    });

    // ================================================================
    //  MODAL XÓA
    // ================================================================
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

    // Submit Xóa
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
                    var deleteModalEl = document.getElementById('deleteModal');
                    var modal = bootstrap.Modal.getInstance(deleteModalEl);
                    if (modal) {
                        modal.hide();
                    }

                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    loadNhanVienData(currentPage);
                } else {
                    alert('Không thể xóa nhân viên này!');
                }
            },
            error: function (xhr) {
                console.log(xhr.responseText);
                alert('Đã xảy ra lỗi khi xóa nhân viên!');
            }
        });
    });
});