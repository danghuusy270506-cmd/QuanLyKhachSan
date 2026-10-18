document.addEventListener('DOMContentLoaded', function () {
    // ================================================================
    //  PHẦN SEARCH & LỌC AJAX (Dùng Form Lọc Chung)
    // ================================================================
    const searchForm = document.getElementById('searchForm');
    const tableContainer = document.getElementById('phongTableContainer');
    const btnClear = document.getElementById('btnClear');

    // Hàm load dữ liệu tổng quát
    window.loadPhongData = function (page = 1) {
        if (!searchForm || !tableContainer) return;

        let baseUrl = searchForm.getAttribute('action') || '/Phong/Index';

        // Lấy toàn bộ dữ liệu từ Form (soPhong, tenLoaiPhong, trangThai...)
        const formData = new FormData(searchForm);
        const params = new URLSearchParams(formData);
        params.append('page', page);

        const url = `${baseUrl}?${params.toString()}`;

        tableContainer.style.opacity = '0.5';

        fetch(url, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(res => {
                if (!res.ok) throw new Error('Lỗi tải dữ liệu');
                return res.text();
            })
            .then(html => {
                tableContainer.innerHTML = html;
                tableContainer.style.opacity = '1';
            })
            .catch(err => {
                console.error(err);
                tableContainer.style.opacity = '1';
                alert('Có lỗi xảy ra khi tải dữ liệu!');
            });
    }

    // Bắt sự kiện Submit Form (Bấm nút Lọc hoặc Enter)
    if (searchForm) {
        searchForm.addEventListener('submit', function (e) {
            e.preventDefault();
            loadPhongData(1);
        });
    }

    // Bắt sự kiện nút Reset / Làm mới
    if (btnClear) {
        btnClear.addEventListener('click', function (e) {
            e.preventDefault();
            searchForm.reset(); // Reset về mặc định (xóa text, đưa select về option đầu tiên)
            loadPhongData(1);
        });
    }

    // Bắt sự kiện Phân trang AJAX
    $(document).on('click', '.custom-pagination .page-link', function (e) {
        e.preventDefault();
        var page = $(this).data('page');
        if (page) {
            loadPhongData(page);
        }
    });

    // ================================================================
    //  MODAL CHI TIẾT
    // ================================================================
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

    // ================================================================
    //  MODAL SỬA
    // ================================================================
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

    // Submit Sửa
    $(document).off('click', '#btnSubmitEdit').on('click', '#btnSubmitEdit', function (e) {
        e.preventDefault();
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
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                if (response && response.success === true) {
                    closeModalAndCleanup('editModal');
                    // Lấy trang hiện tại trước khi load lại
                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    loadPhongData(currentPage);
                } else {
                    $('#editModalContent').html(response);
                }
            },
            error: function () { alert('Có lỗi xảy ra khi lưu dữ liệu!'); }
        });
    });

    // ================================================================
    //  MODAL XÓA
    // ================================================================
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

    // Submit Xóa
    $(document).off('click', '#btnSubmitDelete').on('click', '#btnSubmitDelete', function (e) {
        e.preventDefault();
        var maPhong = $('#deleteMaPhong').val();
        var token = $('#deletePhongContainer input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Phong/Delete/' + maPhong,
            type: 'POST',
            data: { id: maPhong, __RequestVerificationToken: token },
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                closeModalAndCleanup('deleteModal');
                if (response && response.success === true) {
                    var currentPage = $('.custom-pagination .page-item.active .page-link').data('page') || 1;
                    loadPhongData(currentPage);
                } else {
                    alert('Xóa không thành công.');
                }
            },
            error: function () {
                closeModalAndCleanup('deleteModal');
                alert('Có lỗi xảy ra khi thực hiện xóa!');
            }
        });
    });

    // ================================================================
    //  MODAL THÊM MỚI
    // ================================================================
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

    // Submit Thêm Mới
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
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                if (response && response.success === true) {
                    closeModalAndCleanup('createModal');
                    loadPhongData(1);
                } else {
                    $('#createModalContent').html(response);
                }
            },
            error: function () { alert('Có lỗi xảy ra khi lưu dữ liệu!'); }
        });
    });

    // Hàm phụ trợ đóng modal
    function closeModalAndCleanup(modalId) {
        var modalEl = document.getElementById(modalId);
        var modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css({ 'overflow': '', 'padding-right': '' });
    }
});