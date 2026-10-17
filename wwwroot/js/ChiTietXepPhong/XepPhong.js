// wwwroot/js/ChiTietXepPhong/XepPhong.js

// 1. Mở Modal Check-in khi ấn nút
$(document).on('click', '.btn-checkin-ajax', function (e) {
    e.preventDefault();
    var maPhieu = $(this).data('id');

    // Kiểm tra modal tồn tại
    var myModalEl = document.getElementById('xepPhongModal');
    if (!myModalEl) {
        alert('Không tìm thấy modal xếp phòng. Vui lòng kiểm tra lại Index.cshtml.');
        console.error('Missing #xepPhongModal');
        return;
    }

    // Loading
    $('#xepPhongModalBody').html(`
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status"></div>
            <p class="text-muted mt-2">Đang tải dữ liệu...</p>
        </div>
    `);

    var modal = bootstrap.Modal.getOrCreateInstance(myModalEl);
    modal.show();

    // AJAX lấy Partial View Xếp phòng
    $.ajax({
        url: '/ChiTietXepPhong/XepPhong',
        type: 'GET',
        data: { maPhieu: maPhieu },
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (htmlContent) {
            $('#xepPhongModalBody').html(htmlContent);
        },
        error: function (xhr) {
            console.error('XepPhong error:', xhr);
            alert(xhr.responseText || "Có lỗi xảy ra khi tải dữ liệu từ máy chủ.");
            modal.hide();
        }
    });
});

// 2. Bật/tắt nút Xác nhận Check-in khi chọn phòng
$(document).on('change', '#maPhong', function () {
    var selectedVal = $(this).val();
    var submitBtn = $('#btnSubmitCheckIn');
    submitBtn.prop('disabled', !selectedVal);
});

// 3. Submit form xếp phòng qua AJAX
$(document).on('submit', '#formXepPhongAjax', function (e) {
    e.preventDefault();

    var $form = $(this);
    var submitBtn = $('#btnSubmitCheckIn');

    submitBtn.prop('disabled', true).html(
        '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...'
    );

    $.ajax({
        url: $form.attr('action'),
        type: 'POST',
        data: $form.serialize(),
        success: function (response) {
            if (response.success) {
                var myModalEl = document.getElementById('xepPhongModal');
                var modal = bootstrap.Modal.getInstance(myModalEl);
                if (modal) modal.hide();

                updateRowAfterCheckIn(response.maPhieu);
            } else {
                alert(response.message);
                submitBtn.prop('disabled', false).html('<i class="bi bi-check-lg me-1"></i> Xác nhận Check-in');
            }
        },
        error: function (xhr) {
            console.error('Check-in error:', xhr);
            alert(xhr.responseText || "Đã xảy ra lỗi trong quá trình kết nối tới máy chủ.");
            submitBtn.prop('disabled', false).html('<i class="bi bi-check-lg me-1"></i> Xác nhận Check-in');
        }
    });
});

// 4. Cập nhật dòng sau khi Check-in thành công
function updateRowAfterCheckIn(maPhieu) {
    var targetRow = $('tr[data-maphieu="' + maPhieu + '"]');

    if (targetRow.length > 0) {
        var statusCell = targetRow.find('td').eq(5);
        var statusBadge = targetRow.find('.badge-status, .badge-trangthai');

        if (statusBadge.length > 0) {
            statusBadge
                .removeClass('badge-cho-xac-nhan badge-da-xac-nhan badge-hoan-thanh badge-da-huy bg-primary bg-success bg-warning')
                .addClass('badge-status badge-dang-o')
                .text('Đang ở')
                .removeAttr('style');
        } else {
            statusCell.html('<span class="badge-status badge-dang-o">Đang ở</span>');
        }

        var actionTd = targetRow.find('.td-thao-tac');
        var checkoutHtml = `
            <button type="button"
                    class="btn btn-danger btn-sm px-3 fw-medium rounded-pill shadow-sm btn-checkout-ajax"
                    data-id="${maPhieu}">
                Check-out
            </button>
        `;
        actionTd.html(checkoutHtml);
    } else {
        location.reload();
    }
}