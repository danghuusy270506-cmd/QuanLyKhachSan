$(function () {
    // ================================================================
    // 1. TẢI DANH SÁCH & LỌC AJAX
    // ================================================================
    function taiDanhSachHoaDon(page) {
        const duLieu = $("#formLocHoaDon").serializeArray();
        duLieu.push({ name: "page", value: page });

        $.ajax({
            url: typeof urlLayDanhSach !== 'undefined' ? urlLayDanhSach : '/HoaDon/Index',
            type: "GET",
            data: $.param(duLieu),
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (html) {
                $("#danhSachHoaDon").html(html);
            },
            error: function (xhr) {
                console.error(xhr.responseText);
                alert("Không thể tải danh sách hóa đơn. Vui lòng thử lại.");
            }
        });
    }

    // Lọc hoặc tìm kiếm
    $("#formLocHoaDon").on("submit", function (e) {
        e.preventDefault();
        taiDanhSachHoaDon(1);
    });

    // Xóa lọc
    $("#btnXoaLoc").on("click", function () {
        $("#formLocHoaDon")[0].reset();
        taiDanhSachHoaDon(1);
    });

    // Phân trang AJAX
    $(document).on("click", "#danhSachHoaDon .ajax-trang", function (e) {
        e.preventDefault();
        const page = parseInt($(this).attr("data-page"));
        if (!page || $(this).closest(".page-item").hasClass("disabled")) return;
        taiDanhSachHoaDon(page);
    });

    // ================================================================
    // 2. MỞ MODAL CHI TIẾT
    // ================================================================
    $(document).on("click", ".btn-xem-chi-tiet", function () {
        var maHoaDon = $(this).attr("data-id");

        $("#modalContentContainer").html(`
            <div class="modal-body text-center py-5">
                <div class="spinner-border text-primary" role="status"></div>
            </div>
        `);

        var myModal = new bootstrap.Modal(document.getElementById('chiTietHoaDonModal'));
        myModal.show();

        $.ajax({
            url: '/HoaDon/ChiTietHoaDon',
            type: 'GET',
            data: { maHoaDon: maHoaDon },
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                $("#modalContentContainer").html(response);
            },
            error: function () {
                $("#modalContentContainer").html(`<div class="modal-body text-center text-danger py-4">Không thể tải chi tiết hóa đơn.</div>`);
            }
        });
    });

    // ================================================================
    // 3. XÁC NHẬN THANH TOÁN (Cập nhật UI không cần load lại)
    // ================================================================
    $(document).on("click", ".btn-xac-nhan-modal", function () {
        var maHoaDon = $(this).attr("data-id");
        var $btn = $(this);

        $btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...');

        $.ajax({
            url: '/HoaDon/XacNhanThanhToan',
            type: 'POST',
            data: {
                maHoaDon: maHoaDon,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            headers: { "X-Requested-With": "XMLHttpRequest" },
            success: function (response) {
                if (response.success) {
                    // Lấy thời gian hiện tại
                    var now = new Date();
                    var formattedDate = String(now.getDate()).padStart(2, '0') + '/' +
                        String(now.getMonth() + 1).padStart(2, '0') + '/' +
                        now.getFullYear() + ' ' +
                        String(now.getHours()).padStart(2, '0') + ':' +
                        String(now.getMinutes()).padStart(2, '0');

                    // A. CẬP NHẬT GIAO DIỆN BÊN TRONG MODAL
                    // 1. Cập nhật text thời gian
                    $("#modal-thoigian-thanhtoan").text(formattedDate);

                    // 2. Cập nhật Badge Trạng thái (ID: modal-trangthai-thanhtoan)
                    $("#modal-trangthai-thanhtoan").html(`
                        <span class="badge bg-success-subtle text-success border border-success-subtle px-3 py-2 rounded-pill fw-semibold">
                            <i class="bi bi-check-circle-fill me-1"></i> Đã thanh toán
                        </span>
                    `);

                    // 3. Cập nhật Footer (Chữ thanh toán thành công)
                    $("#modal-footer-actions").html(`
                        <span class="badge bg-success-subtle text-success border border-success-subtle px-3 py-2 fw-semibold rounded-pill d-inline-flex align-items-center gap-1.5" style="font-size: 14px;">
                            <i class="bi bi-check-circle-fill text-success"></i> Thanh toán thành công
                        </span>
                    `);

                    // B. CẬP NHẬT GIAO DIỆN NGOÀI BẢNG DANH SÁCH
                    var $targetRow = $("button[data-id='" + maHoaDon + "'], a[data-id='" + maHoaDon + "']").closest('tr');
                    if ($targetRow.length > 0) {
                        // Cột 5: Thời gian
                        $targetRow.find("td").eq(4).text(formattedDate);
                        // Cột 6: Trạng thái
                        $targetRow.find("td").eq(5).html('<span class="badge bg-success">Đã thanh toán</span>');
                        // Cột cuối: Thao tác (đổi thành nút Xác nhận bị vô hiệu hóa)
                        $targetRow.find("td:last").html('<button type="button" class="btn btn-sm btn-success" disabled>Xác nhận</button>');
                    }
                } else {
                    alert(response.message || "Có lỗi xảy ra.");
                    $btn.prop("disabled", false).html('<i class="bi bi-check-circle-fill fs-5"></i> Xác nhận đã nhận tiền');
                }
            },
            error: function () {
                alert("Không thể kết nối đến máy chủ.");
                $btn.prop("disabled", false).html('<i class="bi bi-check-circle-fill fs-5"></i> Xác nhận đã nhận tiền');
            }
        });
    });

    // ================================================================
    // 4. XỬ LÝ XÓA HÓA ĐƠN
    // ================================================================
    $(document).on('click', '.btn-xoa-modal', function (e) {
        e.preventDefault();
        var url = $(this).attr('href');

        $('#modalContentContainer').html(`<div class="modal-body text-center py-4"><div class="spinner-border text-danger" role="status"></div></div>`);

        var myModal = new bootstrap.Modal(document.getElementById('chiTietHoaDonModal'));
        myModal.show();

        $.get(url, function (data) {
            $('#modalContentContainer').html(data);
        }).fail(function () {
            $('#modalContentContainer').html(`<div class="modal-body text-center text-danger py-4">Không thể tải thông tin xóa.</div>`);
        });
    });

    $(document).on('submit', '#formXoaHoaDon', function (e) {
        e.preventDefault();
        var form = $(this);

        $.ajax({
            url: form.attr('action'),
            type: form.attr('method'),
            data: form.serialize(),
            success: function (response) {
                var modalEl = document.getElementById('chiTietHoaDonModal');
                var modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) modal.hide();

                $('.modal-backdrop').remove(); $('body').removeClass('modal-open').css('overflow', '');

                taiDanhSachHoaDon(1);
            },
            error: function () { alert('Có lỗi xảy ra khi xóa hóa đơn!'); }
        });
    });

    // Fix lỗi kẹt màn hình đen
    $(document).on('hidden.bs.modal', '#chiTietHoaDonModal', function () {
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css('overflow', '');
    });
});