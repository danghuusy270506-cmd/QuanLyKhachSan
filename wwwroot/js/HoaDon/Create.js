// Create.js - Xử lý form tạo hóa đơn (HoaDon)
// Đọc giá trị từ data-* attribute trên form → an toàn khi load AJAX

// ===== 1. Lấy giá trị tiền từ form =====
function layGiaTriTuForm() {
    const form = document.getElementById('formHoaDon');
    if (!form) return { tienPhong: 0, tienDichVu: 0, tienCoc: 0 };
    return {
        tienPhong: Number(form.dataset.tienPhong) || 0,
        tienDichVu: Number(form.dataset.tienDichVu) || 0,
        tienCoc: Number(form.dataset.tienCoc) || 0
    };
}

// ===== 2. Tính và cập nhật tổng tiền =====
function capNhatTongTienHoaDon() {
    const inputGiamGia = document.getElementById("GiamGia");
    const tongTienHienThi = document.getElementById("TongTienHienThi");
    if (!inputGiamGia || !tongTienHienThi) return;

    const giamGia = Number(inputGiamGia.value) || 0;
    const { tienPhong, tienDichVu, tienCoc } = layGiaTriTuForm();

    let tongTien = tienPhong + tienDichVu - tienCoc - giamGia;
    if (tongTien < 0) tongTien = 0;

    tongTienHienThi.value = tongTien.toLocaleString("vi-VN") + " VNĐ";
}

// ===== 3. Cập nhật tổng tiền khi nhập giảm giá =====
$(document).on('input', '#GiamGia', function () {
    capNhatTongTienHoaDon();
});

// ===== 4. Validation cho Kiểu thanh toán =====
$(document).on('invalid', '#KieuThanhToan', function () {
    if (this.value === "") {
        this.setCustomValidity("Vui lòng chọn kiểu thanh toán.");
    }
});

$(document).on('change', '#KieuThanhToan', function () {
    this.setCustomValidity("");
});

// ===== 5. Submit form bằng AJAX =====
$(document).on('submit', '#formHoaDon', function (e) {
    e.preventDefault();

    const form = $(this);
    const inputGiamGia = document.getElementById("GiamGia");
    const giamGia = Number(inputGiamGia?.value) || 0;
    const { tienPhong, tienDichVu, tienCoc } = layGiaTriTuForm();
    const tongTien = tienPhong + tienDichVu - tienCoc - giamGia;

    // Validation
    if (!Number.isFinite(giamGia) || giamGia < 0 || tongTien < 0) {
        alert("Giảm giá không hợp lệ hoặc tổng tiền nhỏ hơn 0.");
        inputGiamGia?.focus();
        return;
    }

    // Tìm container để thay thế nội dung sau khi tạo xong
    // Cách A: chỉ có 1 .card duy nhất trên trang (vì Index.cshtml chỉ có partial)
    const container = document.querySelector(".card");

    if (container) container.style.opacity = "0.4";

    $.ajax({
        url: form.attr('action') || '/HoaDon/Create',
        type: 'POST',
        data: form.serialize(),
        headers: { "X-Requested-With": "XMLHttpRequest" },
        success: function (response) {
            if (response && response.success) {
                // Load danh sách hóa đơn ngầm
                $.ajax({
                    url: '/HoaDon/Index',
                    type: 'GET',
                    headers: { "X-Requested-With": "XMLHttpRequest" },
                    success: function (listHtml) {
                        if (container) {
                            container.outerHTML = listHtml; // ⚠️ thay cả .card cũ bằng partial mới
                            // hoặc dùng:
                            // container.innerHTML = listHtml;
                        }
                        window.history.pushState({}, "", '/HoaDon/Index');
                    },
                    error: function () {
                        window.location.href = '/HoaDon/Index';
                    }
                });
            } else {
                if (container) container.style.opacity = "1";
                alert(response.message || 'Có lỗi xảy ra!');
            }
        },
        error: function (xhr) {
            if (container) container.style.opacity = "1";
            alert(xhr.responseText || 'Đã xảy ra lỗi khi tạo hóa đơn!');
        }
    });
});