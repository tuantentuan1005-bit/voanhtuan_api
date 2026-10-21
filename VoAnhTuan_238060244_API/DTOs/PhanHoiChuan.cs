namespace VoAnhTuan_238060244_API.DTOs
{
    /// <summary>
    /// Cấu trúc response chuẩn cho toàn bộ API
    /// </summary>
    public class PhanHoiChuan<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static PhanHoiChuan<T> ThanhCong(T data, string thongBao = "Thành công")
            => new() { Success = true, Message = thongBao, Data = data };

        public static PhanHoiChuan<T> ThatBai(string thongBao)
            => new() { Success = false, Message = thongBao, Data = default };
    }

    public class PhanHoiChuan
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public static PhanHoiChuan ThanhCong(object? data = null, string thongBao = "Thành công")
            => new() { Success = true, Message = thongBao, Data = data };

        public static PhanHoiChuan ThatBai(string thongBao)
            => new() { Success = false, Message = thongBao, Data = null };
    }
}
