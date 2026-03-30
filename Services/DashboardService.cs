using Oracle.ManagedDataAccess.Client;
using LibraryOS.Models;

namespace LibraryOS.Services
{
    public class DashboardService
    {
        private readonly string _conn;

        public DashboardService(IConfiguration config)
        {
            _conn = config.GetConnectionString("OracleDbConnection")!;
        }

        // ════════════════════════════════════════════
        // QUẢN LÝ
        // ════════════════════════════════════════════
        public QuanLyDashboardVM GetQuanLyDashboard()
        {
            var vm = new QuanLyDashboardVM();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            // Stat cards
            vm.TongDauSach = QueryInt(conn, "SELECT COUNT(*) FROM SACH");
            vm.TongCuonSach = QueryInt(conn, "SELECT COUNT(*) FROM CUONSACH WHERE DaAn = 0");
            vm.DangMuon = QueryInt(conn, "SELECT COUNT(*) FROM PHIEUMUON WHERE TinhTrang = N'Đang mượn'");
            vm.TheHopLe = QueryInt(conn, "SELECT COUNT(*) FROM THETHUVIEN WHERE NgayHetHan >= SYSDATE");

            // Phiếu mượn gần đây
            var sql1 = @"
                SELECT pm.maPM, pm.SoTheTV, ttv.HoTenDG,
                       TO_CHAR(pm.NgayMuon,'DD/MM/YYYY') AS NgayMuon,
                       pm.TinhTrang,
                       (SELECT COUNT(*) FROM CT_PHIEUMUON c WHERE c.maPM = pm.maPM) AS SoCuon
                FROM PHIEUMUON pm
                LEFT JOIN THETHUVIEN ttv ON pm.SoTheTV = ttv.SoTheTV
                ORDER BY pm.NgayMuon DESC
                FETCH FIRST 5 ROWS ONLY";

            using (var cmd = new OracleCommand(sql1, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    vm.PhieuMuonGanDay.Add(new PhieuMuonRow
                    {
                        MaPM = r["maPM"].ToString()!,
                        SoTheTV = r["SoTheTV"].ToString()!,
                        HoTenDG = r["HoTenDG"].ToString()!,
                        NgayMuon = r["NgayMuon"].ToString()!,
                        TinhTrang = r["TinhTrang"].ToString()!,
                        SoCuon = Convert.ToInt32(r["SoCuon"]),
                        IsQuaHan = r["TinhTrang"].ToString() == "Quá hạn"
                    });
            }

            // Danh sách nhân viên
            var sql2 = @"
                SELECT nv.MaNV, nv.HoTenNV, nv.GioiTinhNV,
                       nv.SdtNV, nv.EmailNV,
                       TO_CHAR(nv.NgayVaoLam,'DD/MM/YYYY') AS NgayVaoLam,
                       tk.LoaiND
                FROM NHANVIEN nv
                LEFT JOIN TAIKHOAN tk ON tk.MaNV = nv.MaNV
                ORDER BY nv.MaNV";

            using (var cmd = new OracleCommand(sql2, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    vm.DanhSachNV.Add(new NhanVienRow
                    {
                        MaNV = r["MaNV"].ToString()!,
                        HoTenNV = r["HoTenNV"].ToString()!,
                        GioiTinh = r["GioiTinhNV"].ToString()!,
                        SdtNV = r["SdtNV"].ToString()!,
                        EmailNV = r["EmailNV"].ToString()!,
                        NgayVaoLam = r["NgayVaoLam"].ToString()!,
                        VaiTro = r["LoaiND"].ToString() == "1" ? "Quản lý" : "Thủ thư"
                    });
            }

            return vm;
        }

        // ════════════════════════════════════════════
        // THỦ THƯ
        // ════════════════════════════════════════════
        public ThuThuDashboardVM GetThuThuDashboard()
        {
            var vm = new ThuThuDashboardVM();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            // Stat cards
            vm.MuonHomNay = QueryInt(conn,
                "SELECT COUNT(*) FROM PHIEUMUON WHERE TRUNC(NgayMuon) = TRUNC(SYSDATE)");
            vm.TraHomNay = QueryInt(conn,
                "SELECT COUNT(*) FROM CT_PHIEUMUON WHERE TRUNC(NgayTra) = TRUNC(SYSDATE)");
            vm.QuaHan = QueryInt(conn,
                "SELECT COUNT(*) FROM PHIEUMUON WHERE TinhTrang = N'Quá hạn'");
            vm.PhatChoThu = QueryInt(conn,
                "SELECT COUNT(*) FROM PHIEUPHAT WHERE TrangThai = N'Chưa thu'");

            // Phiếu mượn cần xử lý
            var sql1 = @"
                SELECT pm.maPM, pm.SoTheTV, ttv.HoTenDG,
                       TO_CHAR(pm.NgayMuon,'DD/MM/YYYY') AS NgayMuon,
                       pm.TinhTrang,
                       (SELECT COUNT(*) FROM CT_PHIEUMUON c WHERE c.maPM = pm.maPM) AS SoCuon,
                       CASE WHEN pm.TinhTrang = N'Quá hạn' THEN 1 ELSE 0 END AS IsQuaHan
                FROM PHIEUMUON pm
                LEFT JOIN THETHUVIEN ttv ON pm.SoTheTV = ttv.SoTheTV
                WHERE pm.TinhTrang IN (N'Đang mượn', N'Quá hạn')
                ORDER BY IsQuaHan DESC, pm.NgayMuon ASC
                FETCH FIRST 10 ROWS ONLY";

            using (var cmd = new OracleCommand(sql1, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    vm.PhieuCanXuLy.Add(new PhieuMuonRow
                    {
                        MaPM = r["maPM"].ToString()!,
                        SoTheTV = r["SoTheTV"].ToString()!,
                        HoTenDG = r["HoTenDG"].ToString()!,
                        NgayMuon = r["NgayMuon"].ToString()!,
                        TinhTrang = r["TinhTrang"].ToString()!,
                        SoCuon = Convert.ToInt32(r["SoCuon"]),
                        IsQuaHan = Convert.ToInt32(r["IsQuaHan"]) == 1
                    });
            }

            // Phiếu nhập gần đây
            var sql2 = @"
                SELECT pn.MaPN, TO_CHAR(pn.NgayNhap,'DD/MM/YYYY') AS NgayNhap,
                       nv.HoTenNV,
                       (SELECT SUM(SoLuong) FROM CT_PHIEUNHAP ct WHERE ct.MaPN = pn.MaPN) AS SoCuon
                FROM PHIEUNHAP pn
                LEFT JOIN NHANVIEN nv ON pn.MaNV = nv.MaNV
                ORDER BY pn.NgayNhap DESC
                FETCH FIRST 5 ROWS ONLY";

            using (var cmd = new OracleCommand(sql2, conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    vm.PhieuNhapGanDay.Add(new PhieuNhapRow
                    {
                        MaPN = r["MaPN"].ToString()!,
                        NgayNhap = r["NgayNhap"].ToString()!,
                        NhanVien = r["HoTenNV"].ToString()!,
                        SoCuon = r["SoCuon"] == DBNull.Value ? 0 : Convert.ToInt32(r["SoCuon"])
                    });
            }

            return vm;
        }

        // ════════════════════════════════════════════
        // ĐỌC GIẢ
        // ════════════════════════════════════════════
        public DocGiaDashboardVM GetDocGiaDashboard(string soTheTV)
        {
            var vm = new DocGiaDashboardVM();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            // Thông tin thẻ
            var sqlThe = @"
                SELECT HoTenDG, TO_CHAR(NgayHetHan,'DD/MM/YYYY') AS NgayHetHan,
                       CASE WHEN NgayHetHan >= SYSDATE THEN 1 ELSE 0 END AS ConHan
                FROM THETHUVIEN
                WHERE SoTheTV = :soTheTV";

            using (var cmd = new OracleCommand(sqlThe, conn))
            {
                cmd.Parameters.Add("soTheTV", soTheTV);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    vm.HoTen = r["HoTenDG"].ToString()!;
                    vm.SoTheTV = soTheTV;
                    vm.NgayHetHan = r["NgayHetHan"].ToString()!;
                    vm.ConHan = Convert.ToInt32(r["ConHan"]) == 1;
                }
            }

            // Sách đang mượn
            var sql1 = @"
                SELECT ct.MaCuonSach, s.TenSach, ct.maPM,
                       TO_CHAR(pm.NgayMuon,'DD/MM/YYYY') AS NgayMuon,
                       TO_CHAR(ct.NgayTra,'DD/MM/YYYY')  AS NgayTra,
                       ROUND(ct.NgayTra - SYSDATE) AS NgayConLai
                FROM CT_PHIEUMUON ct
                JOIN PHIEUMUON pm ON ct.maPM = pm.maPM
                JOIN CUONSACH  cs ON ct.MaCuonSach = cs.MaCuonSach
                JOIN SACH       s ON cs.maSACH = s.maSACH
                WHERE pm.SoTheTV  = :soTheTV
                  AND pm.TinhTrang = N'Đang mượn'
                ORDER BY ct.NgayTra ASC";

            using (var cmd = new OracleCommand(sql1, conn))
            {
                cmd.Parameters.Add("soTheTV", soTheTV);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    vm.SachDangMuon.Add(new SachDangMuonRow
                    {
                        MaCuonSach = r["MaCuonSach"].ToString()!,
                        TenSach = r["TenSach"].ToString()!,
                        MaPM = r["maPM"].ToString()!,
                        NgayMuon = r["NgayMuon"].ToString()!,
                        NgayTra = r["NgayTra"].ToString()!,
                        NgayConLai = r["NgayConLai"] == DBNull.Value ? 0 : Convert.ToInt32(r["NgayConLai"])
                    });
            }

            vm.SoSachDangMuon = vm.SachDangMuon.Count;

            // Lịch sử mượn
            var sql2 = @"
                SELECT s.TenSach, ct.maPM,
                       TO_CHAR(ct.NgayTra,'DD/MM/YYYY') AS NgayTra,
                       pm.TinhTrang
                FROM CT_PHIEUMUON ct
                JOIN PHIEUMUON pm ON ct.maPM = pm.maPM
                JOIN CUONSACH  cs ON ct.MaCuonSach = cs.MaCuonSach
                JOIN SACH       s ON cs.maSACH = s.maSACH
                WHERE pm.SoTheTV   = :soTheTV
                  AND pm.TinhTrang != N'Đang mượn'
                ORDER BY ct.NgayTra DESC
                FETCH FIRST 5 ROWS ONLY";

            using (var cmd = new OracleCommand(sql2, conn))
            {
                cmd.Parameters.Add("soTheTV", soTheTV);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    vm.LichSuMuon.Add(new LichSuMuonRow
                    {
                        TenSach = r["TenSach"].ToString()!,
                        MaPM = r["maPM"].ToString()!,
                        NgayTra = r["NgayTra"].ToString()!,
                        TinhTrang = r["TinhTrang"].ToString()!
                    });
            }

            return vm;
        }

        // ── Helper ───────────────────────────────────
        private static int QueryInt(OracleConnection conn, string sql)
        {
            using var cmd = new OracleCommand(sql, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value || result == null ? 0 : Convert.ToInt32(result);
        }
    }
}