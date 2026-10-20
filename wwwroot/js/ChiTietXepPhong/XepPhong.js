
$(document).on('click', '.btn-xepphong-ajax', function (e) {
    e.preventDefault();

    const maPhieu = $(this).data('id');
    const modalElement = document.getElementById('xepPhongModal');

    if (!modalElement) {
        alert('Không tìm thấy cửa sổ xếp phòng.');
        return;
    }

    $('#xepPhongModalBody').html(`
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status"></div>
            <p class="text-muted mt-2">Đang tải danh sách phòng...</p>
        </div>
    `);

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    modal.show();

    $.ajax({
        url: '/ChiTietXepPhong/XepPhong',
        type: 'GET',
        data: { maPhieu: maPhieu },
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        success: function (html) {
            $('#xepPhongModalBody').html(html);
        },
        error: function (xhr) {
            const message = xhr.responseText ||
                'Không thể tải danh sách phòng phù hợp.';

            alert(message);
            modal.hide();
        }
    });
});

// Chỉ bật nút lưu xếp phòng khi đã chọn phòng
$(document).on('change', '#maPhong', function () {
    $('#btnSubmitXepPhong').prop('disabled', !$(this).val());
});

// Lưu phòng được phân, không thực hiện check-in
$(document).on('submit', '#formXepPhongAjax', function (e) {
    e.preventDefault();

    const form = $(this);
    const submitButton = $('#btnSubmitXepPhong');

    submitButton.prop('disabled', true).text('Đang xếp phòng...');

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        headers: { 'X-Requested-With': 'XMLHttpRequest' },

        success: function (response) {
            if (!response || !response.success) {
                alert((response && response.message) ||
                    'Không thể xếp phòng.');
                submitButton.prop('disabled', false).text('Xếp phòng');
                return;
            }

            const modalElement = document.getElementById('xepPhongModal');
            const modal = bootstrap.Modal.getInstance(modalElement);

            if (modal) modal.hide();

            updateRowAfterXepPhong(
                response.maPhieu,
                response.soPhong
            );

            alert(response.message || 'Xếp phòng thành công.');
        },

        error: function (xhr) {
            const message = xhr.responseJSON?.message ||
                'Có lỗi xảy ra khi lưu thông tin xếp phòng.';

            alert(message);
            submitButton.prop('disabled', false).text('Xếp phòng');
        }
    });
});

// Sau khi xếp phòng: giữ nguyên trạng thái Đã xác nhận
function updateRowAfterXepPhong(maPhieu, soPhong) {
    const row = $('tr[data-maphieu="' + maPhieu + '"]');

    if (!row.length) {
        location.reload();
        return;
    }

    row.find('.td-so-phong-da-xep').html(`
        <span class="badge bg-light text-dark border">
            ${soPhong || 1}
        </span>
    `);

    row.find('.td-thao-tac').html(`
        <button type="button"
                class="btn btn-primary btn-sm px-3 fw-medium rounded-pill shadow-sm btn-checkin-confirmed-ajax"
                data-id="${maPhieu}">
            Check-in
        </button>
    `);
}

// Check-in là thao tác riêng sau khi đã xếp phòng
$(document).on('click', '.btn-checkin-confirmed-ajax', function (e) {
    e.preventDefault();

    const button = $(this);
    const maPhieu = button.data('id');
    const token = $('input[name="__RequestVerificationToken"]')
        .first().val() || '';

    button.prop('disabled', true).text('Đang check-in...');

    $.ajax({
        url: '/ChiTietXepPhong/CheckIn',
        type: 'POST',
        data: {
            maPhieu: maPhieu,
            __RequestVerificationToken: token
        },
        headers: { 'X-Requested-With': 'XMLHttpRequest' },

        success: function (response) {
            if (!response || !response.success) {
                alert((response && response.message) ||
                    'Check-in thất bại.');
                button.prop('disabled', false).text('Check-in');
                return;
            }

            updateRowAfterCheckIn(maPhieu);
            alert(response.message || 'Check-in thành công.');
        },

        error: function (xhr) {
            const message = xhr.responseJSON?.message ||
                'Không thể thực hiện check-in. Vui lòng thử lại.';

            alert(message);
            button.prop('disabled', false).text('Check-in');
        }
    });
});

// Sau khi check-in mới đổi trạng thái phiếu sang Đang ở
function updateRowAfterCheckIn(maPhieu) {
    const row = $('tr[data-maphieu="' + maPhieu + '"]');

    if (!row.length) {
        location.reload();
        return;
    }

    const statusCell = row.find('.td-trang-thai');

    statusCell.html(`
        <span class="badge-status badge-dang-o">Đang ở</span>
    `);

    row.find('.td-thao-tac').html(`
        <button type="button"
                class="btn btn-danger btn-sm px-3 fw-medium rounded-pill shadow-sm btn-checkout-ajax"
                data-id="${maPhieu}">
            Check-out
        </button>
    `);
}
