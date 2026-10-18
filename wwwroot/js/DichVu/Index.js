document.addEventListener('DOMContentLoaded', function () {
    // ================================================================
    //  PHẦN SEARCH & LỌC AJAX (TỰ ĐỘNG NHẬN DIỆN THAM SỐ)
    // ================================================================
    const inputTuKhoa = document.getElementById('tuKhoa');
    const tableContainer = document.getElementById('tableContainer');
    const btnClear = document.getElementById('btnClear');
    const searchForm = document.getElementById('searchForm');

    function fetchSearchResults() {
        if (!searchForm || !tableContainer || !inputTuKhoa) return;

        // Tự động lấy Action URL (VD: /DichVu)
        const baseUrl = searchForm.getAttribute('action') || window.location.pathname;

        // Tự động lấy tên tham số (name="tuKhoa", name="soPhong", name="searchString"...)
        const paramName = inputTuKhoa.getAttribute('name') || 'tuKhoa';
        const paramValue = inputTuKhoa.value;

        const url = `${baseUrl}?${paramName}=${encodeURIComponent(paramValue)}`;

        // Hiệu ứng loading
        tableContainer.style.opacity = '0.5';

        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(response => {
                if (!response.ok) throw new Error('Lỗi khi tải dữ liệu');
                return response.text();
            })
            .then(html => {
                tableContainer.innerHTML = html;
                tableContainer.style.opacity = '1';
            })
            .catch(error => {
                console.error('Lỗi khi tìm kiếm AJAX:', error);
                tableContainer.style.opacity = '1';
            });
    }

    // Bắt sự kiện khi Submit Form (Bấm nút Lọc hoặc Enter)
    if (searchForm) {
        searchForm.addEventListener('submit', function (e) {
            e.preventDefault();
            fetchSearchResults();
        });
    }

    // Bắt sự kiện khi bấm nút Làm mới (Xóa lọc)
    if (btnClear) {
        btnClear.addEventListener('click', function (e) {
            e.preventDefault();
            if (inputTuKhoa) inputTuKhoa.value = '';
            fetchSearchResults();
        });
    }

    // ================================================================
    //  MODAL CHI TIẾT
    // ================================================================
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-view-detail');
        if (!btn) return;
        e.preventDefault();

        const id = btn.dataset.id;
        if (!id) return;

        const modalBody = document.getElementById('detailModalBody');
        const modalEl = document.getElementById('detailModal');
        if (!modalBody || !modalEl || typeof bootstrap === 'undefined') return;

        modalBody.innerHTML = `<div class="text-center py-4"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Đang tải...</span></div></div>`;
        bootstrap.Modal.getOrCreateInstance(modalEl).show();

        fetch(`/DichVu/Details?madichvu=${encodeURIComponent(id)}`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(response => {
                if (!response.ok) throw new Error('Không thể tải chi tiết');
                return response.text();
            })
            .then(html => { modalBody.innerHTML = html; })
            .catch(error => {
                modalBody.innerHTML = `<div class="alert alert-danger m-3 mb-0">${error.message}</div>`;
            });
    });

    // ================================================================
    //  MODAL SỬA
    // ================================================================
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-edit-modal');
        if (!btn) return;
        e.preventDefault();

        const id = btn.dataset.id;
        if (!id) return;

        const modalBody = document.getElementById('editModalBody');
        const modalEl = document.getElementById('editModal');
        if (!modalBody || !modalEl) return;

        modalBody.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-warning" role="status"></div><div class="mt-2 text-muted">Đang tải form...</div></div>`;
        bootstrap.Modal.getOrCreateInstance(modalEl).show();

        fetch(`/DichVu/Edit?madichvu=${encodeURIComponent(id)}`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(res => {
                if (!res.ok) throw new Error('Không thể tải form sửa');
                return res.text();
            })
            .then(html => { modalBody.innerHTML = html; })
            .catch(err => { modalBody.innerHTML = `<div class="alert alert-danger m-3">${err.message}</div>`; });
    });

    // ================================================================
    //  SUBMIT FORM SỬA
    // ================================================================
    document.addEventListener('submit', function (e) {
        const form = e.target.closest('#editDichVuForm');
        if (!form) return;
        e.preventDefault();

        const btnSave = form.querySelector('#btnSaveEdit');
        const originalText = btnSave.innerHTML;
        btnSave.disabled = true;
        btnSave.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...`;

        const formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(async res => {
                if (res.ok && res.headers.get('content-type')?.includes('application/json')) {
                    return res.json();
                }
                if (res.ok) {
                    const html = await res.text();
                    throw { isValidation: true, html: html };
                }
                throw new Error('Lỗi máy chủ');
            })
            .then(data => {
                if (data.success) {
                    bootstrap.Modal.getInstance(document.getElementById('editModal')).hide();
                    fetchSearchResults();
                }
            })
            .catch(err => {
                btnSave.disabled = false;
                btnSave.innerHTML = originalText;
                if (err.isValidation) {
                    document.getElementById('editModalBody').innerHTML = err.html;
                } else {
                    alert(err.message || 'Có lỗi xảy ra, vui lòng thử lại!');
                }
            });
    });

    // ================================================================
    //  MODAL XÓA
    // ================================================================
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-delete-modal');
        if (!btn) return;
        e.preventDefault();

        const id = btn.dataset.id;
        if (!id) return;

        const modalBody = document.getElementById('deleteModalBody');
        const modalEl = document.getElementById('deleteModal');
        if (!modalBody || !modalEl) return;

        modalBody.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-danger" role="status"></div><div class="mt-2 text-muted">Đang tải...</div></div>`;
        bootstrap.Modal.getOrCreateInstance(modalEl).show();

        fetch(`/DichVu/Delete?madichvu=${encodeURIComponent(id)}`, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(res => {
                if (!res.ok) throw new Error('Không thể tải dữ liệu xóa');
                return res.text();
            })
            .then(html => { modalBody.innerHTML = html; })
            .catch(err => { modalBody.innerHTML = `<div class="alert alert-danger m-3">${err.message}</div>`; });
    });

    // ================================================================
    //  SUBMIT FORM XÓA
    // ================================================================
    document.addEventListener('submit', function (e) {
        const form = e.target.closest('#deleteDichVuForm');
        if (!form) return;
        e.preventDefault();

        const btnConfirm = form.querySelector('#btnConfirmDelete');
        const originalText = btnConfirm.innerHTML;
        btnConfirm.disabled = true;
        btnConfirm.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span> Đang xử lý...`;

        const formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(async res => {
                if (res.headers.get('content-type')?.includes('application/json')) {
                    return res.json();
                }
                throw new Error('Lỗi phản hồi từ máy chủ');
            })
            .then(data => {
                if (data.success) {
                    bootstrap.Modal.getInstance(document.getElementById('deleteModal')).hide();
                    fetchSearchResults();
                } else {
                    alert(data.message || 'Không thể xóa dịch vụ này.');
                    btnConfirm.disabled = false;
                    btnConfirm.innerHTML = originalText;
                }
            })
            .catch(err => {
                alert('Có lỗi xảy ra trong quá trình xóa!');
                btnConfirm.disabled = false;
                btnConfirm.innerHTML = originalText;
            });
    });

    // ================================================================
    //  MỞ MODAL THÊM MỚI
    // ================================================================
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.btn-create-modal');
        if (!btn) return;
        e.preventDefault();

        const modalBody = document.getElementById('createModalBody');
        const modalEl = document.getElementById('createModal');
        if (!modalBody || !modalEl) return;

        modalBody.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status"></div>
                <div class="mt-2 text-muted">Đang tải form...</div>
            </div>
        `;

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

        fetch('/DichVu/Create', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(res => {
                if (!res.ok) throw new Error('Không thể tải form thêm mới');
                return res.text();
            })
            .then(html => {
                modalBody.innerHTML = html;
            })
            .catch(err => {
                modalBody.innerHTML = `<div class="alert alert-danger m-3">${err.message}</div>`;
            });
    });

    // ================================================================
    //  SUBMIT FORM THÊM MỚI
    // ================================================================
    document.addEventListener('submit', function (e) {
        const form = e.target.closest('#createDichVuForm');
        if (!form) return;
        e.preventDefault();

        const btnSave = form.querySelector('#btnCreateDichVu');
        const originalText = btnSave.innerHTML;
        btnSave.disabled = true;
        btnSave.innerHTML = `<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...`;

        const formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(async res => {
                const ct = res.headers.get('content-type') || '';
                if (ct.includes('application/json')) {
                    return res.json();
                }
                const html = await res.text();
                throw { isValidation: true, html: html };
            })
            .then(data => {
                if (data.success) {
                    bootstrap.Modal.getInstance(document.getElementById('createModal')).hide();
                    fetchSearchResults();
                }
            })
            .catch(err => {
                btnSave.disabled = false;
                btnSave.innerHTML = originalText;
                if (err.isValidation) {
                    document.getElementById('createModalBody').innerHTML = err.html;
                } else {
                    alert(err.message || 'Có lỗi xảy ra, vui lòng thử lại!');
                }
            });
    });
});