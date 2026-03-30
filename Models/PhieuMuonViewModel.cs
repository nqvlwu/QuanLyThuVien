namespace LibraryOS.Models
{
    public class TaoPhieuMuonVM
    {
        // ── Input từ form ──────────────────────────
        public string SoTheTV { get; set; } = "";
        public string NgayMuon { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public string NgayTra { get; set; } = DateTime.Now.AddDays(14).ToString("yyyy-MM-dd");
        public string GhiChu { get; set; } = "";
        public List<string> DanhSachCuonSach { get; set; } = new(); // mã cuốn được chọn

        // ── Hiển thị trên form ──────────────────────
        public string HoTenDG { get; set; } = ""; // tự điền sau khi nhập SoTheTV
        public bool TheHopLe { get; set; } = true;
        public string ThongBao { get; set; } = "";
        public bool ThanhCong { get; set; } = false;

        // ── Danh sách sách để chọn ──────────────────
        public List<CuonSachChoMuon> DanhSachSach { get; set; } = new();
    }

    public class CuonSachChoMuon
    {
        public string MaCuonSach { get; set; } = "";
        public string MaSach { get; set; } = "";
        public string TenSach { get; set; } = "";
        public string TacGia { get; set; } = "";
        public string TheLoai { get; set; } = "";
        public bool DuocChon { get; set; } = false;
    }
}