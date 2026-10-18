$(function () {
    // Tải lại bảng hóa đơn bằng AJAX (có hỗ trợ phân trang và bộ lọc)
    function taiDanhSachHoaDon(page) {
        const duLieu = $("#formLocHoaDon").serializeArray();

        duLieu.push({
            name: "page",
            value: page
        });

        $.ajax({
            url: typeof urlLayDanhSach !== 'undefined' ? urlLayDanhSach : '/HoaDon/Index',
            type: "GET",
            data: $.param(duLieu),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (html) {
                $("#danhSachHoaDon").html(html);
            },
            error: function (xhr) {
                console.error(xhr.responseText);
                alert("Không thể tải danh sách hóa đơn. Vui lòng thử lại.");
            }
        });
    }

    // Lọc hoặc tìm kiếm: quay về trang đầu tiên
    $("#formLocHoaDon").on("submit", function (e) {
        e.preventDefault();
        taiDanhSachHoaDon(1);
    });

    // Bấm nút xóa lọc
    $("#btnXoaLoc").on("click", function () {
        $("#formLocHoaDon")[0].reset();
        taiDanhSachHoaDon(1);
    });

    // Phân trang: dùng sự kiện delegation vì bảng được thay bằng AJAX
    $(document).on(
        "click",
        "#danhSachHoaDon .ajax-trang",
        function (e) {
            e.preventDefault();

            const page = parseInt($(this).attr("data-page"));

            if (!page || $(this).closest(".page-item").hasClass("disabled")) {
                return;
            }

            taiDanhSachHoaDon(page);
        }
    );

    // Sự kiện bấm mở Modal chi tiết (dùng chung cho cả nút Chi tiết và nút Thanh toán ngoài bảng)
    $(document).on("click", ".btn-xem-chi-tiet", function () {
        var maHoaDon = $(this).attr("data-id");

        // Hiển thị khung chờ (loading) trong modal trước khi load dữ liệu
        $("#modalContentContainer").html(`
            <div class="modal-body text-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            </div>
        `);

        // Mở modal
        var myModal = new bootstrap.Modal(document.getElementById('chiTietHoaDonModal'));
        myModal.show();

        // Gọi AJAX lấy nội dung chi tiết
        $.ajax({
            url: '/HoaDon/ChiTietHoaDon',
            type: 'GET',
            data: { maHoaDon: maHoaDon },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                $("#modalContentContainer").html(response);
            },
            error: function (xhr) {
                console.error(xhr.responseText);
                $("#modalContentContainer").html(`
                    <div class="modal-body text-center text-danger py-4">
                        Không thể tải dữ liệu chi tiết hóa đơn. Vui lòng thử lại.
                    </div>
                `);
            }
        });
    });

    // Sự kiện bấm nút Xác nhận thanh toán NẰM BÊN TRONG Modal
    $(document).on("click", ".btn-xac-nhan-modal", function () {
        var maHoaDon = $(this).attr("data-id");

        var $btn = $(this);
        // Giữ nguyên thiết kế và icon của nút khi chuyển sang trạng thái "Đang xử lý..."
        $btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Đang xử lý...');

        $.ajax({
            url: '/HoaDon/XacNhanThanhToan',
            type: 'POST',
            data: {
                maHoaDon: maHoaDon,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                if (response.success) {
                    // Tạo chuỗi thời gian hiện tại (định dạng dd/MM/yyyy HH:mm)
                    var now = new Date();
                    var formattedDate = String(now.getDate()).padStart(2, '0') + '/' +
                        String(now.getMonth() + 1).padStart(2, '0') + '/' +
                        now.getFullYear() + ' ' +
                        String(now.getHours()).padStart(2, '0') + ':' +
                        String(now.getMinutes()).padStart(2, '0');

                    // A. Cập nhật giao diện bên trong Modal
                    $("#modal-trangthai-thanhtoan").html('<span class="badge bg-success">Đã thanh toán</span>');
                    $("#modal-thoigian-thanhtoan").text(formattedDate);
                    $("#modal-footer-actions").html(`
                    <span class="badge bg-success bg-opacity-10 text-success border border-success border-opacity-25 px-3 py-2 fw-semibold rounded-pill d-inline-flex align-items-center gap-1.5" style="font-size: 13.5px;">
                        <i class="bi bi-check-circle-fill text-success"></i> Thanh toán thành công
                    </span>
                `);

                    // B. Cập nhật giao diện ngoài bảng danh sách chính theo đúng hàng (tr) chứa hóa đơn:
                    var $targetRow = $("button[data-id='" + maHoaDon + "'], a[data-id='" + maHoaDon + "']").closest('tr');

                    if ($targetRow.length > 0) {
                        // 1. Cập nhật cột "Thời gian thanh toán" (cột thứ 5)
                        $targetRow.find("td").eq(4).text(formattedDate);

                        // 2. Cập nhật cột "Trạng thái" (cột thứ 6) thành nhãn "Đã thanh toán"
                        $targetRow.find("td").eq(5).html('<span class="badge bg-success">Đã thanh toán</span>');

                        // 3. Cập nhật cột thao tác ngoài cùng
                        $targetRow.find("td:last").html('<button type="button" class="btn btn-sm btn-success" disabled>Xác nhận</button>');
                    }
                } else {
                    alert(response.message || "Có lỗi xảy ra.");
                    $btn.prop("disabled", false).html('<i class="bi bi-check-circle-fill fs-5"></i> Xác nhận đã nhận tiền');
                }
            },
            error: function (xhr) {
                console.error(xhr.responseText);
                alert("Không thể kết nối đến máy chủ.");
                $btn.prop("disabled", false).html('<i class="bi bi-check-circle-fill fs-5"></i> Xác nhận đã nhận tiền');
            }
        });
    });

    // Khắc phục triệt để lỗi kẹt màn hình đen (backdrop) của Bootstrap Modal khi đóng
    $(document).on('hidden.bs.modal', '#chiTietHoaDonModal', function () {
        $('.modal-backdrop').remove(); $('body').removeClass('modal-open').css('overflow', '');
    });

    // Sự kiện bấm nút Xóa mở Modal AJAX
    $(document).on('click', '.btn-xoa-modal', function (e) {
        e.preventDefault(); // Ngăn trình duyệt chuyển trang

        var url = $(this).attr('href'); // Lấy đường dẫn /HoaDon/Delete?maHoaDon=...

        // Hiển thị icon xoay/spinner trong Modal trước khi tải dữ liệu xong
        $('#modalContentContainer').html(`
            <div class="modal-body text-center py-4">
                <div class="spinner-border text-danger" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            </div>
        `);

        // Mở Modal
        var myModal = new bootstrap.Modal(document.getElementById('chiTietHoaDonModal'));
        myModal.show();

        // Gọi AJAX lấy Partial View xác nhận xóa
        $.get(url, function (data) {
            $('#modalContentContainer').html(data);
        }).fail(function () {
            $('#modalContentContainer').html(`
                <div class="modal-body text-center text-danger py-4">
                    <p>Không thể tải thông tin xóa hóa đơn. Vui lòng thử lại!</p>
                </div>
            `);
        });
    });

    // Sự kiện gửi form Xóa trong Modal
    $(document).on('submit', '#formXoaHoaDon', function (e) {
        e.preventDefault();
        var form = $(this);

        $.ajax({
            url: form.attr('action'),
            type: form.attr('method'),
            data: form.serialize(),
            success: function (response) {
                // Đóng Modal
                var modalEl = document.getElementById('chiTietHoaDonModal');
                var modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) {
                    modal.hide();
                }

                // Dọn dẹp backdrop đề phòng lỗi kẹt lớp phủ tối màn hình
                $('.modal-backdrop').remove(); $('body').removeClass('modal-open').css('overflow', '');

                // Gọi lại hàm tải danh sách AJAX chính xác
                if (typeof taiDanhSachHoaDon === 'function') {
                    taiDanhSachHoaDon(1);
                } else {
                    location.reload();
                }
            },
            error: function () {
                alert('Có lỗi xảy ra khi xóa hóa đơn!');
            }
        });
    });

});