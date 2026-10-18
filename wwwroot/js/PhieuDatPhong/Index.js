// Hàm dùng chung gọi AJAX fetch dữ liệu bảng mượt mà
function fetchData(url) {
    const container = document.getElementById("phieuDatPhongContainer");
    if (!container) return;

    container.style.opacity = "0.5";

    fetch(url, {
        headers: { "X-Requested-With": "XMLHttpRequest" }
    })
        .then(response => {
            if (!response.ok) throw new Error("Lỗi khi tải dữ liệu.");
            return response.text();
        })
        .then(html => {
            container.innerHTML = html;
            container.style.opacity = "1";
            window.history.pushState({}, "", url);
        })
        .catch(error => {
            console.error("AJAX Error:", error);
            container.style.opacity = "1";
            alert("Đã xảy ra lỗi khi tải dữ liệu!");
        });
}

document.addEventListener("DOMContentLoaded", function () {
    const container = document.getElementById("phieuDatPhongContainer");
    if (!container) return;

    const filterForm = document.getElementById("filterForm");

    if (filterForm) {
        filterForm.addEventListener("submit", function (e) {
            e.preventDefault();
            const formData = new FormData(filterForm);
            const queryString = new URLSearchParams(formData).toString();
            fetchData(`${filterForm.action}?${queryString}`);
        });
    }

    document.addEventListener("click", function (e) {
        const pageLink = e.target.closest("#phieuDatPhongContainer .pagination a");
        if (pageLink) {
            e.preventDefault();
            const url = pageLink.getAttribute("href");
            if (url) fetchData(url);
            return;
        }

        const btnReset = e.target.closest("#btnResetFilter");
        if (btnReset && filterForm) {
            e.preventDefault();
            filterForm.querySelectorAll('input[type="text"], input[type="date"]').forEach(input => input.value = '');
            filterForm.querySelectorAll('select').forEach(select => select.selectedIndex = 0);
            fetchData(filterForm.action);
            return;
        }
    });
});

function openModal(url, data = {}) {
    $.ajax({
        url: url,
        type: 'GET',
        data: data,
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (response) {
            if (typeof response === 'object' && response.success === false) {
                alert(response.message || 'Có lỗi xảy ra.');
                return;
            }

            $('#detailModalContent').html(response);
            var myModalEl = document.getElementById('detailModal');

            var myModal = bootstrap.Modal.getInstance(myModalEl);
            if (!myModal) {
                myModal = new bootstrap.Modal(myModalEl, {
                    backdrop: true,
                    keyboard: true
                });
            }
            myModal.show();
        },
        error: function (xhr) {
            alert('Không thể tải dữ liệu. Lỗi: ' + xhr.statusText);
        }
    });
}

$(document).on('click', '.btn-create-phieu', function () {
    openModal('/PhieuDatPhong/Create');
});

$(document).on('click', '.btn-view-details', function () {
    openModal('/PhieuDatPhong/Details', { maphieu: $(this).data('id') });
});

$(document).on('click', '.btn-edit-details', function () {
    openModal('/PhieuDatPhong/Edit', { maphieu: $(this).data('id') });
});

$(document).on('click', '.btn-delete-details', function () {
    openModal('/PhieuDatPhong/Delete', { maphieu: $(this).data('id') });
});

$(document).on('submit', '#createPhieuForm', function (e) {
    e.preventDefault();
    var form = $(this);

    $.ajax({
        url: form.attr('action') || '/PhieuDatPhong/Create',
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            if (response && response.success) {
                var modal = bootstrap.Modal.getInstance(document.getElementById('detailModal'));
                if (modal) modal.hide();
                fetchData(window.location.pathname + window.location.search);
            } else {
                $('#detailModalContent').html(response);
            }
        },
        error: function () {
            alert('Đã xảy ra lỗi khi tạo phiếu đặt phòng!');
        }
    });
});

$(document).on('submit', '#editPhieuForm', function (e) {
    e.preventDefault();
    var form = $(this);

    $.ajax({
        url: form.attr('action') || '/PhieuDatPhong/Edit',
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            if (response && response.success) {
                var modal = bootstrap.Modal.getInstance(document.getElementById('detailModal'));
                if (modal) modal.hide();
                fetchData(window.location.pathname + window.location.search);
            } else {
                $('#detailModalContent').html(response);
            }
        },
        error: function () {
            alert('Đã xảy ra lỗi khi lưu thay đổi!');
        }
    });
});

$(document).on('submit', '#deletePhieuForm', function (e) {
    e.preventDefault();
    var form = $(this);

    $.ajax({
        url: form.attr('action') || '/PhieuDatPhong/Delete',
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            if (response && response.success) {
                var modal = bootstrap.Modal.getInstance(document.getElementById('detailModal'));
                if (modal) modal.hide();
                fetchData(window.location.pathname + window.location.search);
            } else {
                alert(response.message || 'Xóa không thành công!');
            }
        },
        error: function () {
            alert('Đã xảy ra lỗi khi xóa!');
        }
    });
});

$(document).on('click', '.btn-checkout-ajax', function (e) {
    e.preventDefault();
    var btn = $(this);
    var maPhieu = btn.data('id');

    btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...');

    var token = $('input[name="__RequestVerificationToken"]').first().val() || '';

    $.ajax({
        url: '/ChiTietXepPhong/CheckOut',
        type: 'POST',
        data: { maPhieu: maPhieu, __RequestVerificationToken: token },
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (response) {
            if (!response || !response.success) {
                alert((response && response.message) || 'Check-out thất bại!');
                btn.prop('disabled', false).text('Check-out');
                return;
            }
            window.location.href = '/HoaDon/Create?maPhieu=' + maPhieu;
        },
        error: function (xhr) {
            alert(xhr.responseText || 'Lỗi kết nối khi check-out!');
            btn.prop('disabled', false).text('Check-out');
        }
    });
});

$(document).on('click', '.btn-xac-nhan', function (e) {
    e.preventDefault();
    var btn = $(this);
    var maPhieu = btn.data('id');

    btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...');

    var token = $('input[name="__RequestVerificationToken"]').first().val() || '';

    $.ajax({
        url: '/PhieuDatPhong/XacNhan',
        type: 'POST',
        data: { maphieu: maPhieu, __RequestVerificationToken: token },
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (response) {
            if (response && response.success) {
                fetchData(window.location.pathname + window.location.search);
            } else {
                alert((response && response.message) || 'Xác nhận thất bại!');
                btn.prop('disabled', false).text('Xác nhận');
            }
        },
        error: function () {
            alert('Lỗi kết nối khi xác nhận!');
            btn.prop('disabled', false).text('Xác nhận');
        }
    });
});

$(document).on('change', '#ngayDenInput', function () {
    var ngayDen = $(this).val();
    if (ngayDen) {
        $('#ngayDiInput').attr('min', ngayDen);
        var ngayDi = $('#ngayDiInput').val();
        if (ngayDi && ngayDi < ngayDen) {
            $('#ngayDiInput').val(ngayDen);
        }
    }
});

// =====================================================================
// XỬ LÝ THÊM / XÓA DỊCH VỤ TỪ MODAL CHI TIẾT
// =====================================================================

// 1. Bấm nút "Thêm dịch vụ" trong Modal Chi tiết
$(document).on('click', '.btn-add-service', function (e) {
    e.preventDefault();
    var maChiTiet = $(this).data('id');
    var maPhieu = $(this).data('maphieu');

    var modalContent = $('#detailModalContent');
    var modalDialog = modalContent.closest('.modal-dialog');

    // Phóng to Modal
    modalDialog.addClass('modal-lg');

    modalContent.html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><div class="mt-2 text-muted">Đang tải form...</div></div>');

    $.ajax({
        url: '/SuDungDichVu/Create',
        type: 'GET',
        data: { maChiTietXepPhong: maChiTiet, maPhieu: maPhieu },
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (html) {
            modalContent.html(html);
        },
        error: function () {
            modalContent.html('<div class="alert alert-danger m-3">Không thể tải form thêm dịch vụ.</div>');
        }
    });
});

// 2. Submit form thêm dịch vụ
$(document).on('submit', '#formAddService', function (e) {
    e.preventDefault();
    var form = $(this);
    var btnSave = $('#btnSaveService');
    var originalText = btnSave.html();

    btnSave.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...');

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (response) {
            if (response && response.success) {
                var maPhieu = form.find('input[name="maPhieu"]').val();

                // Thu nhỏ modal lại và load lại nội dung chi tiết
                $('#detailModal .modal-dialog').removeClass('modal-lg');

                $.ajax({
                    url: '/PhieuDatPhong/Details',
                    type: 'GET',
                    data: { maphieu: maPhieu },
                    headers: { "X-Requested-With": "XMLHttpRequest" },
                    success: function (html) {
                        $('#detailModalContent').html(html);
                        fetchData(window.location.pathname + window.location.search);
                    }
                });
            } else {
                $('#detailModalContent').html(response);
            }
        },
        error: function () {
            alert('Đã xảy ra lỗi khi thêm dịch vụ!');
            btnSave.prop('disabled', false).html(originalText);
        }
    });
});

// 3. Nút Hủy trong form thêm dịch vụ
$(document).on('click', '.btn-cancel-add-service', function (e) {
    e.preventDefault();
    var maPhieu = $(this).data('maphieu');

    $('#detailModal .modal-dialog').removeClass('modal-lg');

    $.ajax({
        url: '/PhieuDatPhong/Details',
        type: 'GET',
        data: { maphieu: maPhieu },
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (html) {
            $('#detailModalContent').html(html);
        },
        error: function () {
            alert('Lỗi khi tải lại chi tiết phiếu!');
        }
    });
});

// 4. Xử lý nút Xóa Dịch vụ (Thùng rác đỏ)
$(document).on('click', '.btn-delete-service', function (e) {
    e.preventDefault();

    if (!confirm('Bạn có chắc chắn muốn xóa dịch vụ này khỏi phòng? Hành động này không thể hoàn tác.')) {
        return;
    }

    var btn = $(this);
    var id = btn.data('id');
    var maPhieu = btn.data('maphieu');

    btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status"></span>');

    // Lấy token chống giả mạo
    var token = $('input[name="__RequestVerificationToken"]').first().val() || '';

    $.ajax({
        url: '/SuDungDichVu/Delete',
        type: 'POST',
        data: { id: id, __RequestVerificationToken: token },
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (response) {
            if (response && response.success) {
                // Load lại giao diện chi tiết phiếu để cập nhật danh sách
                $.ajax({
                    url: '/PhieuDatPhong/Details',
                    type: 'GET',
                    data: { maphieu: maPhieu },
                    headers: { "X-Requested-With": "XMLHttpRequest" },
                    success: function (html) {
                        $('#detailModalContent').html(html);
                        fetchData(window.location.pathname + window.location.search);
                    }
                });
            } else {
                alert(response.message || 'Xóa dịch vụ thất bại!');
                btn.prop('disabled', false).html('<i class="bi bi-trash3"></i>');
            }
        },
        error: function () {
            alert('Lỗi kết nối khi xóa dịch vụ!');
            btn.prop('disabled', false).html('<i class="bi bi-trash3"></i>');
        }
    });
});