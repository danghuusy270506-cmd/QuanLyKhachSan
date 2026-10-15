document.addEventListener("DOMContentLoaded", function () {
    const tienPhongEl = document.getElementById("TienPhong");
    const tienDichVuEl = document.getElementById("TienDichVu");
    const tienCocEl = document.getElementById("TienCoc");
    const giamGiaEl = document.getElementById("GiamGia");
    const tongTienEl = document.getElementById("TongTien");

    // Tính tổng tiền khi thay đổi giảm giá
    function tinhTongTien() {
        const tienPhong = Number(tienPhongEl.value) || 0;
        const tienDichVu = Number(tienDichVuEl.value) || 0;
        const tienCoc = Number(tienCocEl.value) || 0;
        const giamGia = Number(giamGiaEl.value) || 0;

        const tongTien = Math.max(
            0,
            tienPhong + tienDichVu - tienCoc - giamGia
        );

        tongTienEl.value = tongTien;
    }

    giamGiaEl.addEventListener("input", tinhTongTien);
    tinhTongTien();

    // Kiểm tra kiểu thanh toán
    const form = document.querySelector("form");
    const selectThanhToan =
        document.getElementById("KieuThanhToan");
    const loiThanhToan =
        document.getElementById("loiThanhToan");

    form.addEventListener("submit", function (event) {
        if (selectThanhToan.value === "") {
            event.preventDefault();
            loiThanhToan.style.display = "block";
            selectThanhToan.focus();
        }
    });

    selectThanhToan.addEventListener("change", function () {
        if (selectThanhToan.value !== "") {
            loiThanhToan.style.display = "none";
        }
    });
});