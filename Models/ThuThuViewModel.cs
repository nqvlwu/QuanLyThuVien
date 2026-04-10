namespace LibraryOS.Models
{
    public class PhieuNhapChiTiet
    {
        public string MaPN { get; set; } = "";
        public string NgayNhap { get; set; } = "";
        public string HoTenNV { get; set; } = "";
        public int SoDauSach { get; set; }
        public int TongCuon { get; set; }
        public long TongTien { get; set; }
    }

    public class CTPhieuNhap
    {
        public string MaSach { get; set; } = "";
        public string TenSach { get; set; } = "";
        public int SoLuong { get; set; }
        public long DonGia { get; set; }
        public long ThanhTien { get; set; }
    }

    public class PhieuPhatRow
    {
        public string MaPP { get; set; } = "";
        public string MaPM { get; set; } = "";
        public string HoTenDG { get; set; } = "";
        public string SoTheTV { get; set; } = "";
        public string LyDo { get; set; } = "";
        public long SoTienPhat { get; set; }
        public string TrangThai { get; set; } = "";
        public string NgayLap { get; set; } = "";
    }

    public class TheThuVienRow
    {
        public string SoTheTV { get; set; } = "";
        public string HoTenDG { get; set; } = "";
        public string GioiTinh { get; set; } = "";
        public string NgayBatDau { get; set; } = "";
        public string NgayHetHan { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public int DangMuon { get; set; }
    }
}