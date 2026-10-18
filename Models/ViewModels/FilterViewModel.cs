namespace QuanLyKhachSan.Models.ViewModels // Chú ý chữ ViewModels có 's'
{
    public class FilterViewModel
    {
        public string ActionUrl { get; set; } = "";
        public string Placeholder { get; set; } = "Nhập từ khóa...";
        public string SearchParamName { get; set; } = "tuKhoa";
        public string? SearchValue { get; set; }
    }
}