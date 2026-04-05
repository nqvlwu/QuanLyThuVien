namespace LibraryOS.Models
{
    public class SachRow
    {
        public string MaSach { get; set; } = "";
        public string TenSach { get; set; } = "";
        public long GiaSach { get; set; }
        public int NamXB { get; set; }
        public string TenNXB { get; set; } = "";
        public string TacGia { get; set; } = "";
        public string TheLoai { get; set; } = "";
        public int TongCuon { get; set; }
        public int ConLai { get; set; }
    }

    public class NhaXuatBanRow
    {
        public string MaNXB { get; set; } = "";
        public string TenNXB { get; set; } = "";
        public string EmailNXB { get; set; } = "";
        public string DiaChiNXB { get; set; } = "";
        public string NguoiDaiDien { get; set; } = "";
        public int SoSach { get; set; }
    }

    public class TacGiaRow
    {
        public string MaTG { get; set; } = "";
        public string HoTenTG { get; set; } = "";
        public int SoSach { get; set; }
    }

    public class TheLoaiRow
    {
        public string MaTL { get; set; } = "";
        public string TenTL { get; set; } = "";
        public int SoSach { get; set; }
    }
    public class BaoCaoSachMuonRow
    {
        public string MaSach { get; set; } = "";
        public string TenSach { get; set; } = "";
        public string TenNXB { get; set; } = "";
        public string TacGia { get; set; } = "";
        public string TheLoai { get; set; } = "";
        public int TongLuotMuon { get; set; }
        public int DangMuon { get; set; }
        public int DaTra { get; set; }
    }
}