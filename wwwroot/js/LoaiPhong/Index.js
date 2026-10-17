// Hàm gọi AJAX tải dữ liệu danh sách Loại Phòng theo trang
function loadLoaiPhongData(page = 1) {
    $.ajax({
        url: '/LoaiPhong/Index',
        type: 'GET',
        data: { page: page },
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        },
        success: function (result) {
            $('#loaiPhongTableContainer').html(result);
        },
        error: function (xhr, status, error) {
            console.log("Error: " + error);
            alert('Có lỗi xảy ra khi tải dữ liệu loại phòng!');
        }
    });
}

// Bắt sự kiện click phân trang AJAX cho Loại Phòng
$(document).on('click', '.loaiphong-page-link', function (e) {
    e.preventDefault();
    var page = $(this).data('page');
    if (page) {
        loadLoaiPhongData(page);
    }
});