using Oracle.ManagedDataAccess.Client;
using LibraryOS.Models;

namespace LibraryOS.Services
{
    public class PhieuMuonService
    {
        private readonly string _conn;

        public PhieuMuonService(IConfiguration config)
        {
            _conn = config.GetConnectionString("OracleDbConnection")!;
        }

        // ── Kiểm tra thẻ thư viện ──────────────────
        public (string HoTen, bool HopLe) KiemTraThe(string soTheTV)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
                SELECT HoTenDG,
                       CASE WHEN NgayHetHan >= SYSDATE THEN 1 ELSE 0 END AS HopLe
                FROM THETHUVIEN
                WHERE SoTheTV = :soTheTV";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("soTheTV", soTheTV);
            using var r = cmd.ExecuteReader();

            if (!r.Read()) return ("Không tìm thấy thẻ", false);
            return (r["HoTenDG"].ToString()!, Convert.ToInt32(r["HopLe"]) == 1);
        }
        public List<PhieuMuonRow> GetDanhSachPhieuMuon(string? tinhTrang = null)
        {
            var list = new List<PhieuMuonRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
        SELECT pm.maPM, pm.SoTheTV, ttv.HoTenDG,
               TO_CHAR(pm.NgayMuon,'DD/MM/YYYY') AS NgayMuon,
               pm.TinhTrang,
               (SELECT COUNT(*) FROM CT_PHIEUMUON c WHERE c.maPM = pm.maPM) AS SoCuon,
               nv.HoTenNV AS TenNV
        FROM PHIEUMUON pm
        LEFT JOIN THETHUVIEN ttv ON pm.SoTheTV  = ttv.SoTheTV
        LEFT JOIN NHANVIEN   nv  ON pm.MaNV     = nv.MaNV
        WHERE (:tinhTrang IS NULL OR pm.TinhTrang = :tinhTrang)
        ORDER BY pm.NgayMuon DESC";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("tinhTrang", (object?)tinhTrang ?? DBNull.Value);
            cmd.Parameters.Add("tinhTrang", (object?)tinhTrang ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new PhieuMuonRow
                {
                    MaPM = r["maPM"].ToString()!,
                    SoTheTV = r["SoTheTV"].ToString()!,
                    HoTenDG = r["HoTenDG"].ToString()!,
                    NgayMuon = r["NgayMuon"].ToString()!,
                    TinhTrang = r["TinhTrang"].ToString()!,
                    SoCuon = Convert.ToInt32(r["SoCuon"]),
                    IsQuaHan = r["TinhTrang"].ToString() == "Quá hạn",
                    TenNV = r["TenNV"].ToString()!,
                });

            return list;
        }
        // ── Lấy danh sách cuốn sách còn có thể mượn ─
        public List<CuonSachChoMuon> GetSachCoThe()
        {
            var list = new List<CuonSachChoMuon>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
                SELECT cs.MaCuonSach, s.maSACH, s.TenSach,
                       (SELECT LISTAGG(tg.HoTenTG, ', ')
                        FROM TACGIA_SACH tgs
                        JOIN TACGIA tg ON tgs.maTG = tg.maTG
                        WHERE tgs.maSACH = s.maSACH) AS TacGia,
                       (SELECT LISTAGG(tl.TenTL, ', ')
                        FROM THELOAI_SACH tls
                        JOIN THELOAI tl ON tls.MaTL = tl.MaTL
                        WHERE tls.maSACH = s.maSACH) AS TheLoai
                FROM CUONSACH cs
                JOIN SACH s ON cs.maSACH = s.maSACH
                WHERE cs.TinhTrang = 1    -- còn trong kho
                  AND cs.DaAn     = 0    -- chưa bị ẩn
                ORDER BY s.TenSach, cs.MaCuonSach";

            using var cmd = new OracleCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new CuonSachChoMuon
                {
                    MaCuonSach = r["MaCuonSach"].ToString()!,
                    MaSach     = r["maSACH"].ToString()!,
                    TenSach    = r["TenSach"].ToString()!,
                    TacGia     = r["TacGia"].ToString()!,
                    TheLoai    = r["TheLoai"].ToString()!,
                });
            return list;
        }

        // ── Sinh mã phiếu mượn mới ──────────────────
        private string SinhMaPM(OracleConnection conn)
        {
            var sql = "SELECT NVL(MAX(TO_NUMBER(SUBSTR(maPM,3))),0)+1 FROM PHIEUMUON";
            using var cmd = new OracleCommand(sql, conn);
            var next = Convert.ToInt32(cmd.ExecuteScalar());
            return "PM" + next.ToString("D3");
        }

        // ── Tạo phiếu mượn ─────────────────────────
        public (bool Ok, string ThongBao) TaoPhieuMuon(
            string soTheTV,
            string maNV,
            DateTime ngayMuon,
            DateTime ngayTra,
            List<string> danhSachCuon,
            string ghiChu)
        {
            if (!danhSachCuon.Any())
                return (false, "Vui lòng chọn ít nhất 1 cuốn sách.");

            if (danhSachCuon.Count > 5)
                return (false, "Chỉ được mượn tối đa 5 cuốn mỗi lần.");

            using var conn = new OracleConnection(_conn);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                // Kiểm tra độc giả đang mượn bao nhiêu cuốn
                var sqlDangMuon = @"
                    SELECT COUNT(*) FROM CT_PHIEUMUON ct
                    JOIN PHIEUMUON pm ON ct.maPM = pm.maPM
                    WHERE pm.SoTheTV  = :soTheTV
                      AND pm.TinhTrang = N'Đang mượn'";

                using (var cmd = new OracleCommand(sqlDangMuon, conn))
                {
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("soTheTV", soTheTV);
                    var dangMuon = Convert.ToInt32(cmd.ExecuteScalar());
                    if (dangMuon + danhSachCuon.Count > 5)
                        return (false, $"Độc giả đang mượn {dangMuon} cuốn, không thể mượn thêm {danhSachCuon.Count} cuốn (tối đa 5).");
                }

                var maPM = SinhMaPM(conn);

                // Insert PHIEUMUON
                var sqlPM = @"
                    INSERT INTO PHIEUMUON (maPM, NgayMuon, TinhTrang, MaNV, SoTheTV)
                    VALUES (:maPM, :ngayMuon, N'Đang mượn', :maNV, :soTheTV)";

                using (var cmd = new OracleCommand(sqlPM, conn))
                {
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("maPM",    maPM);
                    cmd.Parameters.Add("ngayMuon", ngayMuon);
                    cmd.Parameters.Add("maNV",    maNV);
                    cmd.Parameters.Add("soTheTV", soTheTV);
                    cmd.ExecuteNonQuery();
                }

                // Insert CT_PHIEUMUON + cập nhật TinhTrang cuốn sách
                foreach (var maCuon in danhSachCuon)
                {
                    var sqlCT = @"
                        INSERT INTO CT_PHIEUMUON (maPM, MaCuonSach, NgayTra, GhiChu)
                        VALUES (:maPM, :maCuon, :ngayTra, :ghiChu)";

                    using (var cmd = new OracleCommand(sqlCT, conn))
                    {
                        cmd.Transaction = tran;
                        cmd.Parameters.Add("maPM",   maPM);
                        cmd.Parameters.Add("maCuon", maCuon);
                        cmd.Parameters.Add("ngayTra", ngayTra);
                        cmd.Parameters.Add("ghiChu", string.IsNullOrEmpty(ghiChu) ? DBNull.Value : (object)ghiChu);
                        cmd.ExecuteNonQuery();
                    }

                    // Đánh dấu cuốn sách là đang được mượn
                    var sqlCS = "UPDATE CUONSACH SET TinhTrang = 0 WHERE MaCuonSach = :maCuon";
                    using (var cmd = new OracleCommand(sqlCS, conn))
                    {
                        cmd.Transaction = tran;
                        cmd.Parameters.Add("maCuon", maCuon);
                        cmd.ExecuteNonQuery();
                    }
                }

                tran.Commit();
                return (true, $"Tạo phiếu mượn {maPM} thành công! ({danhSachCuon.Count} cuốn)");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return (false, $"Lỗi: {ex.Message}");
            }
        }
    }
}