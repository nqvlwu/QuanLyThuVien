using Oracle.ManagedDataAccess.Client;
using LibraryOS.Models;

namespace LibraryOS.Services
{
    public class ThuThuService
    {
        private readonly string _conn;
        public ThuThuService(IConfiguration config)
        {
            _conn = config.GetConnectionString("OracleDbConnection")!;
        }

        // ═══════════════════════════════════════
        // PHIẾU NHẬP
        // ═══════════════════════════════════════
        public List<PhieuNhapChiTiet> GetPhieuNhap()
        {
            var list = new List<PhieuNhapChiTiet>();
            using var conn = new OracleConnection(_conn);
            conn.Open();
            var sql = @"
                SELECT pn.MaPN,
                       TO_CHAR(pn.NgayNhap,'DD/MM/YYYY') AS NgayNhap,
                       nv.HoTenNV,
                       COUNT(ct.maSACH)                  AS SoDauSach,
                       NVL(SUM(ct.SoLuong),0)            AS TongCuon,
                       NVL(SUM(ct.SoLuong * ct.DonGia),0) AS TongTien
                FROM PHIEUNHAP pn
                LEFT JOIN NHANVIEN    nv ON pn.MaNV   = nv.MaNV
                LEFT JOIN CT_PHIEUNHAP ct ON ct.MaPN  = pn.MaPN
                GROUP BY pn.MaPN, pn.NgayNhap, nv.HoTenNV
                ORDER BY pn.NgayNhap DESC";
            using var cmd = new OracleCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new PhieuNhapChiTiet
                {
                    MaPN = r["MaPN"].ToString()!,
                    NgayNhap = r["NgayNhap"].ToString()!,
                    HoTenNV = r["HoTenNV"].ToString()!,
                    SoDauSach = Convert.ToInt32(r["SoDauSach"]),
                    TongCuon = Convert.ToInt32(r["TongCuon"]),
                    TongTien = Convert.ToInt64(r["TongTien"]),
                });
            return list;
        }

        public List<CTPhieuNhap> GetCTPhieuNhap(string maPN)
        {
            var list = new List<CTPhieuNhap>();
            using var conn = new OracleConnection(_conn);
            conn.Open();
            var sql = @"
                SELECT ct.maSACH, s.TenSach, ct.SoLuong, ct.DonGia,
                       ct.SoLuong * ct.DonGia AS ThanhTien
                FROM CT_PHIEUNHAP ct
                JOIN SACH s ON ct.maSACH = s.maSACH
                WHERE ct.MaPN = :maPN";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("maPN", maPN);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new CTPhieuNhap
                {
                    MaSach = r["maSACH"].ToString()!,
                    TenSach = r["TenSach"].ToString()!,
                    SoLuong = Convert.ToInt32(r["SoLuong"]),
                    DonGia = Convert.ToInt64(r["DonGia"]),
                    ThanhTien = Convert.ToInt64(r["ThanhTien"]),
                });
            return list;
        }

        // ═══════════════════════════════════════
        // PHIẾU PHẠT
        // ═══════════════════════════════════════
        public List<PhieuPhatRow> GetPhieuPhat(string? trangThai = null)
        {
            var list = new List<PhieuPhatRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();
            var sql = @"
                SELECT pp.maPP, pp.LyDo, pp.SoTienPhat, pp.TrangThai,
                       TO_CHAR(pp.NgayLap,'DD/MM/YYYY') AS NgayLap,
                       pp.maPM,
                       ttv.HoTenDG, pm.SoTheTV
                FROM PHIEUPHAT pp
                JOIN PHIEUMUON   pm  ON pp.maPM     = pm.maPM
                JOIN THETHUVIEN  ttv ON pm.SoTheTV   = ttv.SoTheTV
                WHERE (:tt IS NULL OR pp.TrangThai   = :tt)
                ORDER BY pp.NgayLap DESC";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("tt", (object?)trangThai ?? DBNull.Value);
            cmd.Parameters.Add("tt", (object?)trangThai ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new PhieuPhatRow
                {
                    MaPP = r["maPP"].ToString()!,
                    MaPM = r["maPM"].ToString()!,
                    HoTenDG = r["HoTenDG"].ToString()!,
                    SoTheTV = r["SoTheTV"].ToString()!,
                    LyDo = r["LyDo"].ToString()!,
                    SoTienPhat = Convert.ToInt64(r["SoTienPhat"]),
                    TrangThai = r["TrangThai"].ToString()!,
                    NgayLap = r["NgayLap"].ToString()!,
                });
            return list;
        }

        public (bool Ok, string ThongBao) LapPhieuPhat(
            string maPM, string lyDo, long soTien)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            try
            {
                // Sinh mã PP mới
                var sqlMa = "SELECT NVL(MAX(TO_NUMBER(SUBSTR(maPP,3))),0)+1 FROM PHIEUPHAT";
                int next;
                using (var cmd = new OracleCommand(sqlMa, conn))
                    next = Convert.ToInt32(cmd.ExecuteScalar());
                var maPP = "PP" + next.ToString("D3");

                var sql = @"
                    INSERT INTO PHIEUPHAT (maPP, NgayLap, LyDo, SoTienPhat, TrangThai, maPM)
                    VALUES (:maPP, SYSDATE, :lyDo, :soTien, N'Chưa thu', :maPM)";
                using var cmd2 = new OracleCommand(sql, conn);
                cmd2.Parameters.Add("maPP", maPP);
                cmd2.Parameters.Add("lyDo", lyDo);
                cmd2.Parameters.Add("soTien", soTien);
                cmd2.Parameters.Add("maPM", maPM);
                cmd2.ExecuteNonQuery();

                // Cập nhật trạng thái phiếu mượn
                var sqlPM = "UPDATE PHIEUMUON SET TinhTrang = N'Quá hạn' WHERE maPM = :maPM";
                using var cmd3 = new OracleCommand(sqlPM, conn);
                cmd3.Parameters.Add("maPM", maPM);
                cmd3.ExecuteNonQuery();

                return (true, $"Lập phiếu phạt {maPP} thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        public (bool Ok, string ThongBao) ThuTienPhat(string maPP)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            try
            {
                var sql = @"UPDATE PHIEUPHAT SET TrangThai = N'Đã thu'
                            WHERE maPP = :maPP";
                using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add("maPP", maPP);
                cmd.ExecuteNonQuery();
                return (true, "Đã thu tiền phạt thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════
        // THẺ THƯ VIỆN
        // ═══════════════════════════════════════
        public List<TheThuVienRow> GetTheTV(string? keyword = null, string? trangThai = null)
        {
            var list = new List<TheThuVienRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();
            var sql = @"
                SELECT SoTheTV, HoTenDG, GioiTinhDG,
                       TO_CHAR(NgayBatDau,'DD/MM/YYYY') AS NgayBatDau,
                       TO_CHAR(NgayHetHan,'DD/MM/YYYY') AS NgayHetHan,
                       CASE WHEN NgayHetHan >= SYSDATE THEN 'Còn hạn' ELSE 'Hết hạn' END AS TrangThai,
                       (SELECT COUNT(*) FROM PHIEUMUON pm
                        WHERE pm.SoTheTV = ttv.SoTheTV
                          AND pm.TinhTrang = N'Đang mượn') AS DangMuon
                FROM THETHUVIEN ttv
                WHERE (:kw IS NULL
                   OR LOWER(HoTenDG) LIKE '%'||LOWER(:kw)||'%'
                   OR LOWER(SoTheTV) LIKE '%'||LOWER(:kw)||'%')
                AND (:tt IS NULL
                   OR ((:tt = 'Còn hạn' AND NgayHetHan >= SYSDATE)
                   OR  (:tt = 'Hết hạn' AND NgayHetHan < SYSDATE)))
                ORDER BY NgayHetHan DESC";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);
            cmd.Parameters.Add("tt", (object?)trangThai ?? DBNull.Value);
            cmd.Parameters.Add("tt", (object?)trangThai ?? DBNull.Value);
            cmd.Parameters.Add("tt", (object?)trangThai ?? DBNull.Value);
            cmd.Parameters.Add("tt", (object?)trangThai ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new TheThuVienRow
                {
                    SoTheTV = r["SoTheTV"].ToString()!,
                    HoTenDG = r["HoTenDG"].ToString()!,
                    GioiTinh = r["GioiTinhDG"].ToString()!,
                    NgayBatDau = r["NgayBatDau"].ToString()!,
                    NgayHetHan = r["NgayHetHan"].ToString()!,
                    TrangThai = r["TrangThai"].ToString()!,
                    DangMuon = Convert.ToInt32(r["DangMuon"]),
                });
            return list;
        }
    }
}