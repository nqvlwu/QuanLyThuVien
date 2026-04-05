namespace LibraryOS.Models
{
    // ── Quản lý ──────────────────────────────────
    public class QuanLyDashboardVM
    {
        public int TongDauSach { get; set; }
        public int TongCuonSach { get; set; }
        public int DangMuon { get; set; }
        public int TheHopLe { get; set; }
        public List<PhieuMuonRow> PhieuMuonGanDay { get; set; } = new();
        public List<NhanVienRow> DanhSachNV { get; set; } = new();
    }

    // ── Thủ thư ───────────────────────────────────
    public class ThuThuDashboardVM
    {
        public int MuonHomNay { get; set; }
        public int TraHomNay { get; set; }
        public int QuaHan { get; set; }
        public int PhatChoThu { get; set; }
        public List<PhieuMuonRow> PhieuCanXuLy { get; set; } = new();
        public List<PhieuNhapRow> PhieuNhapGanDay { get; set; } = new();
    }

    // ── Đọc giả ───────────────────────────────────
    public class DocGiaDashboardVM
    {
        public string HoTen { get; set; } = "";
        public string SoTheTV { get; set; } = "";
        public string NgayHetHan { get; set; } = "";
        public bool ConHan { get; set; }
        public int SoSachDangMuon { get; set; }
        public List<SachDangMuonRow> SachDangMuon { get; set; } = new();
        public List<LichSuMuonRow> LichSuMuon { get; set; } = new();
    }

    // ── Shared rows ───────────────────────────────
    public class PhieuMuonRow
    {
        public string MaPM { get; set; } = "";
        public string SoTheTV { get; set; } = "";
        public string HoTenDG { get; set; } = "";
        public string NgayMuon { get; set; } = "";
        public string TinhTrang { get; set; } = "";
        public string TenNV { get; set; } = "";
        public int SoCuon { get; set; }
        public bool IsQuaHan { get; set; }
    }

    // Thêm vào DashboardViewmodel.cs
    public class NhanVienRow
    {
        public string MaNV { get; set; } = "";
        public string HoTenNV { get; set; } = "";
        public string GioiTinh { get; set; } = "";
        public string SdtNV { get; set; } = "";
        public string EmailNV { get; set; } = "";
        public string DiaChiNV { get; set; } = "";
        public string NgaySinh { get; set; } = "";
        public string NgayVaoLam { get; set; } = "";
        public string VaiTro { get; set; } = "";
    }

    public class PhieuNhapRow
    {
        public string MaPN { get; set; } = "";
        public string NgayNhap { get; set; } = "";
        public string NhanVien { get; set; } = "";
        public int SoCuon { get; set; }
    }

    public class SachDangMuonRow
    {
        public string MaCuonSach { get; set; } = "";
        public string TenSach { get; set; } = "";
        public string MaPM { get; set; } = "";
        public string NgayMuon { get; set; } = "";
        public string NgayTra { get; set; } = "";
        public int NgayConLai { get; set; }
    }

    public class LichSuMuonRow
    {
        public string TenSach { get; set; } = "";
        public string MaPM { get; set; } = "";
        public string NgayTra { get; set; } = "";
        public string TinhTrang { get; set; } = "";
    }
}