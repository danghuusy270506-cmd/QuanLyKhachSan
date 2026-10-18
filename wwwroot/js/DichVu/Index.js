document.addEventListener('DOMContentLoaded', function () {
    const inputTuKhoa = document.getElementById('tuKhoa');
    const tableContainer = document.getElementById('tableContainer');
    const resetContainer = document.getElementById('resetBtnContainer');
    const btnClear = document.getElementById('btnClear');
    const totalCountEl = document.getElementById('totalCount');
    const searchForm = document.getElementById('searchForm');

    if (!inputTuKhoa || !tableContainer || !searchForm) return;

    // Hàm gửi AJAX tìm kiếm / lọc dữ liệu
    function fetchSearchResults(tuKhoa) {
        const baseUrl = searchForm.getAttribute('action') || '/DichVu';
        const url = `${baseUrl}?tuKhoa=${encodeURIComponent(tuKhoa)}`;

        fetch(url, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Lỗi khi tải dữ liệu');
                }
                return response.text();
            })
            .then(html => {
                // Cập nhật HTML phần bảng
                tableContainer.innerHTML = html;

                // Ẩn/hiện nút "Xóa lọc" dựa trên từ khóa tìm kiếm
                if (resetContainer) {
                    resetContainer.style.display = tuKhoa.trim().length > 0 ? 'inline-block' : 'none';
                }

                // Cập nhật lại tổng số lượng dịch vụ
                if (totalCountEl) {
                    const emptyRow = tableContainer.querySelector('.service-empty');
                    if (emptyRow) {
                        totalCountEl.innerText = '0';
                    } else {
                        const rows = tableContainer.querySelectorAll('tbody tr');
                        totalCountEl.innerText = rows.length;
                    }
                }
            })
            .catch(error => {
                console.error('Lỗi khi tìm kiếm AJAX:', error);
            });
    }

    // Xử lý sự kiện khi ấn nút "Tìm kiếm" hoặc nhấn Enter trong form
    searchForm.addEventListener('submit', function (e) {
        e.preventDefault(); // Ngăn nộp form / load lại trang
        fetchSearchResults(inputTuKhoa.value);
    });

    // Xử lý sự kiện khi ấn nút "Xóa lọc" bằng AJAX (không load lại trang)
    if (btnClear) {
        btnClear.addEventListener('click', function (e) {
            e.preventDefault(); // Ngăn chuyển trang / reload
            inputTuKhoa.value = ''; // Xóa sạch từ khóa trong ô input
            fetchSearchResults(''); // Gọi AJAX lấy danh sách ban đầu
        });
    }
});