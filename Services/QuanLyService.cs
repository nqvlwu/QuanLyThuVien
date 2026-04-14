using Oracle.ManagedDataAccess.Client;
using LibraryOS.Models;

namespace LibraryOS.Services
{
    public class QuanLyService
    {
        private readonly string _conn;
        public QuanLyService(IConfiguration config)
        {
            _conn = config.GetConnectionString("OracleDbConnection")!;
        }

        // ═══════════════════════════════════════
        // NHÂN VIÊN
        // ═══════════════════════════════════════
        public List<NhanVienRow> GetNhanVien(string? keyword = null)
        {
            var list = new List<NhanVienRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
                SELECT nv.MaNV, nv.HoTenNV, nv.GioiTinhNV, nv.SdtNV,
                       nv.EmailNV, nv.DiaChiNV,
                       TO_CHAR(nv.NgaySinhNV,'DD/MM/YYYY')  AS NgaySinh,
                       TO_CHAR(nv.NgayVaoLam,'DD/MM/YYYY')  AS NgayVaoLam,
                       tk.LoaiND
                FROM NHANVIEN nv
                LEFT JOIN TAIKHOAN tk ON tk.MaNV = nv.MaNV
                WHERE (:kw IS NULL
                   OR LOWER(nv.HoTenNV) LIKE '%' || LOWER(:kw) || '%'
                   OR LOWER(nv.MaNV)    LIKE '%' || LOWER(:kw) || '%')
                ORDER BY nv.MaNV";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new NhanVienRow
                {
                    MaNV = r["MaNV"].ToString()!,
                    HoTenNV = r["HoTenNV"].ToString()!,
                    GioiTinh = r["GioiTinhNV"].ToString()!,
                    SdtNV = r["SdtNV"].ToString()!,
                    EmailNV = r["EmailNV"].ToString()!,
                    DiaChiNV = r["DiaChiNV"].ToString()!,
                    NgaySinh = r["NgaySinh"].ToString()!,
                    NgayVaoLam = r["NgayVaoLam"].ToString()!,
                    VaiTro = r["LoaiND"].ToString() == "1" ? "Quản lý" : "Thủ thư"
                });
            return list;
        }

        // ═══════════════════════════════════════
        // SÁCH
        // ═══════════════════════════════════════
        public List<SachRow> GetSach(string? keyword = null, string? maTL = null)
        {
            var list = new List<SachRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
        SELECT s.maSACH, s.TenSach, s.GiaSach, s.NamXB,
               s.DaAn,
               n.TenNXB,
               (SELECT COUNT(*) FROM CUONSACH c
                WHERE c.maSACH = s.maSACH AND c.DaAn = 0) AS TongCuon,
               (SELECT COUNT(*) FROM CUONSACH c
                WHERE c.maSACH = s.maSACH
                  AND c.TinhTrang = 1 AND c.DaAn = 0) AS ConLai,
               (SELECT LISTAGG(tg.HoTenTG, ', ')
                FROM TACGIA_SACH ts JOIN TACGIA tg ON ts.maTG = tg.maTG
                WHERE ts.maSACH = s.maSACH) AS TacGia,
               (SELECT LISTAGG(tl.TenTL, ', ')
                FROM THELOAI_SACH tls JOIN THELOAI tl ON tls.MaTL = tl.MaTL
                WHERE tls.maSACH = s.maSACH) AS TheLoai
        FROM SACH s
        LEFT JOIN NHAXUATBAN n ON s.maNXB = n.maNXB
        WHERE (:kw IS NULL
           OR LOWER(s.TenSach) LIKE '%' || LOWER(:kw) || '%'
           OR LOWER(s.maSACH)  LIKE '%' || LOWER(:kw) || '%')
        AND (:maTL IS NULL OR EXISTS (
            SELECT 1 FROM THELOAI_SACH tls
            WHERE tls.maSACH = s.maSACH AND tls.MaTL = :maTL))
        ORDER BY s.DaAn ASC, s.maSACH ASC";
            // ↑ sách chưa xóa lên trước, sách đã xóa xuống dưới

    using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);
            cmd.Parameters.Add("maTL", (object?)maTL ?? DBNull.Value);
            cmd.Parameters.Add("maTL", (object?)maTL ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new SachRow
                {
                    MaSach = r["maSACH"].ToString()!,
                    TenSach = r["TenSach"].ToString()!,
                    GiaSach = r["GiaSach"] == DBNull.Value ? 0 : Convert.ToInt64(r["GiaSach"]),
                    NamXB = r["NamXB"] == DBNull.Value ? 0 : Convert.ToInt32(r["NamXB"]),
                    TenNXB = r["TenNXB"].ToString()!,
                    TacGia = r["TacGia"].ToString()!,
                    TheLoai = r["TheLoai"].ToString()!,
                    TongCuon = Convert.ToInt32(r["TongCuon"]),
                    ConLai = Convert.ToInt32(r["ConLai"]),
                    DaAn = Convert.ToInt32(r["DaAn"]) == 1,  // ← thêm
                });
            return list;
        }
        // ═══════════════════════════════════════
        // KHÔI PHỤC SÁCH
        // ═══════════════════════════════════════
        public (bool Ok, string ThongBao) KhoiPhucSach(string maSach)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            try
            {
                using (var cmd = new OracleCommand(
                    "UPDATE SACH SET DaAn = 0 WHERE maSACH = :maSach", conn))
                {
                    cmd.Parameters.Add("maSach", maSach);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new OracleCommand(
                    "UPDATE CUONSACH SET DaAn = 0 WHERE maSACH = :maSach", conn))
                {
                    cmd.Parameters.Add("maSach", maSach);
                    cmd.ExecuteNonQuery();
                }
                return (true, $"Khôi phục sách {maSach} thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════
        // THÊM SÁCH MỚI
        // ═══════════════════════════════════════
        public (bool Ok, string ThongBao) ThemSach(
            string maSach, string tenSach, long giaSach,
            int namXB, string maNXB, string maTL, string maTG, int soLuong)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                // Kiểm tra mã sách đã tồn tại chưa
                using (var cmd = new OracleCommand(
                    "SELECT COUNT(*) FROM SACH WHERE maSACH = :maSach", conn))
                {
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("maSach", maSach);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                        return (false, $"Mã sách '{maSach}' đã tồn tại!");
                }

                // Insert SACH
                var sql1 = @"INSERT INTO SACH (maSACH, TenSach, GiaSach, NamXB, maNXB, DaAn)
                     VALUES (:maSach, :tenSach, :giaSach, :namXB, :maNXB, 0)";
                using (var cmd = new OracleCommand(sql1, conn))
                {
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("maSach", maSach);
                    cmd.Parameters.Add("tenSach", tenSach);
                    cmd.Parameters.Add("giaSach", giaSach);
                    cmd.Parameters.Add("namXB", namXB);
                    cmd.Parameters.Add("maNXB", maNXB);
                    cmd.ExecuteNonQuery();
                }

                // Insert THELOAI_SACH
                if (!string.IsNullOrEmpty(maTL))
                {
                    var sql2 = "INSERT INTO THELOAI_SACH (MaTL, maSACH) VALUES (:maTL, :maSach)";
                    using var cmd = new OracleCommand(sql2, conn);
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("maTL", maTL);
                    cmd.Parameters.Add("maSach", maSach);
                    cmd.ExecuteNonQuery();
                }

                // Insert TACGIA_SACH
                if (!string.IsNullOrEmpty(maTG))
                {
                    var sql3 = "INSERT INTO TACGIA_SACH (maTG, maSACH) VALUES (:maTG, :maSach)";
                    using var cmd = new OracleCommand(sql3, conn);
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("maTG", maTG);
                    cmd.Parameters.Add("maSach", maSach);
                    cmd.ExecuteNonQuery();
                }

                // Insert CUONSACH theo số lượng
                for (int i = 0; i < soLuong; i++)
                {
                    var sql4 = "INSERT INTO CUONSACH (maSACH, TinhTrang, DaAn) VALUES (:maSach, 1, 0)";
                    using var cmd = new OracleCommand(sql4, conn);
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("maSach", maSach);
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                return (true, $"Thêm sách '{tenSach}' thành công! ({soLuong} cuốn)");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════
        // SỬA SÁCH
        // ═══════════════════════════════════════
        public (bool Ok, string ThongBao) SuaSach(
            string maSach, string tenSach, long giaSach, int namXB, string maNXB)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            try
            {
                var sql = @"UPDATE SACH
                    SET TenSach = :tenSach,
                        GiaSach = :giaSach,
                        NamXB   = :namXB,
                        maNXB   = :maNXB
                    WHERE maSACH = :maSach";
                using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add("tenSach", tenSach);
                cmd.Parameters.Add("giaSach", giaSach);
                cmd.Parameters.Add("namXB", namXB);
                cmd.Parameters.Add("maNXB", maNXB);
                cmd.Parameters.Add("maSach", maSach);
                cmd.ExecuteNonQuery();
                return (true, "Cập nhật sách thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        // Lấy thông tin 1 sách để sửa
        public SachRow? GetSachById(string maSach)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            var sql = @"
        SELECT s.maSACH, s.TenSach, s.GiaSach, s.NamXB, s.DaAn,
               s.maNXB, n.TenNXB,
               (SELECT COUNT(*) FROM CUONSACH c WHERE c.maSACH=s.maSACH AND c.DaAn=0) AS TongCuon,
               (SELECT COUNT(*) FROM CUONSACH c WHERE c.maSACH=s.maSACH AND c.TinhTrang=1 AND c.DaAn=0) AS ConLai,
               (SELECT LISTAGG(tg.HoTenTG,', ') FROM TACGIA_SACH ts JOIN TACGIA tg ON ts.maTG=tg.maTG WHERE ts.maSACH=s.maSACH) AS TacGia,
               (SELECT LISTAGG(tl.TenTL,', ')  FROM THELOAI_SACH tls JOIN THELOAI tl ON tls.MaTL=tl.MaTL WHERE tls.maSACH=s.maSACH) AS TheLoai
        FROM SACH s LEFT JOIN NHAXUATBAN n ON s.maNXB=n.maNXB
        WHERE s.maSACH = :maSach";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("maSach", maSach);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new SachRow
            {
                MaSach = r["maSACH"].ToString()!,
                TenSach = r["TenSach"].ToString()!,
                GiaSach = r["GiaSach"] == DBNull.Value ? 0 : Convert.ToInt64(r["GiaSach"]),
                NamXB = r["NamXB"] == DBNull.Value ? 0 : Convert.ToInt32(r["NamXB"]),
                TenNXB = r["TenNXB"].ToString()!,
                TacGia = r["TacGia"].ToString()!,
                TheLoai = r["TheLoai"].ToString()!,
                TongCuon = Convert.ToInt32(r["TongCuon"]),
                ConLai = Convert.ToInt32(r["ConLai"]),
                DaAn = Convert.ToInt32(r["DaAn"]) == 1,
                MaNXB = r["maNXB"].ToString()!,
            };
        }
        // ═══════════════════════════════════════
        // NHÀ XUẤT BẢN
        // ═══════════════════════════════════════
        public List<NhaXuatBanRow> GetNhaXuatBan(string? keyword = null)
        {
            var list = new List<NhaXuatBanRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
                SELECT n.maNXB, n.TenNXB, n.EmailNXB, n.DiaChiNXB, n.NguoiDaiDien,
                       COUNT(s.maSACH) AS SoSach
                FROM NHAXUATBAN n
                LEFT JOIN SACH s ON s.maNXB = n.maNXB
                WHERE (:kw IS NULL
                   OR LOWER(n.TenNXB)        LIKE '%' || LOWER(:kw) || '%'
                   OR LOWER(n.NguoiDaiDien)  LIKE '%' || LOWER(:kw) || '%')
                GROUP BY n.maNXB, n.TenNXB, n.EmailNXB, n.DiaChiNXB, n.NguoiDaiDien
                ORDER BY n.maNXB";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new NhaXuatBanRow
                {
                    MaNXB = r["maNXB"].ToString()!,
                    TenNXB = r["TenNXB"].ToString()!,
                    EmailNXB = r["EmailNXB"].ToString()!,
                    DiaChiNXB = r["DiaChiNXB"].ToString()!,
                    NguoiDaiDien = r["NguoiDaiDien"].ToString()!,
                    SoSach = Convert.ToInt32(r["SoSach"]),
                });
            return list;
        }

        // Lấy danh sách tác giả của 1 đầu sách
        public List<TacGiaRow> GetTacGiaCuaSach(string maSach)
        {
            var list = new List<TacGiaRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
        SELECT tg.maTG, tg.HoTenTG
        FROM TACGIA_SACH ts
        JOIN TACGIA tg ON ts.maTG = tg.maTG
        WHERE ts.maSACH = :maSach
        ORDER BY tg.maTG";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("maSach", maSach);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new TacGiaRow
                {
                    MaTG = r["maTG"].ToString()!,
                    HoTenTG = r["HoTenTG"].ToString()!,
                });
            return list;
        }

        // Thêm tác giả vào sách
        public (bool Ok, string ThongBao) ThemTacGiaVaoSach(string maTG, string maSach)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            try
            {
                // Kiểm tra đã tồn tại chưa
                var sqlCheck = @"SELECT COUNT(*) FROM TACGIA_SACH
                         WHERE maTG = :maTG AND maSACH = :maSach";
                using (var cmd = new OracleCommand(sqlCheck, conn))
                {
                    cmd.Parameters.Add("maTG", maTG);
                    cmd.Parameters.Add("maSach", maSach);
                    var exists = Convert.ToInt32(cmd.ExecuteScalar());
                    if (exists > 0)
                        return (false, "Tác giả này đã được liên kết với sách rồi.");
                }

                var sql = "INSERT INTO TACGIA_SACH (maTG, maSACH) VALUES (:maTG, :maSach)";
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("maTG", maTG);
                    cmd.Parameters.Add("maSach", maSach);
                    cmd.ExecuteNonQuery();
                }
                return (true, "Thêm tác giả thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }
        // ═══════════════════════════════════════
        // TÁC GIẢ
        // ═══════════════════════════════════════
        public List<TacGiaRow> GetTacGia(string? keyword = null)
        {
            var list = new List<TacGiaRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
                SELECT tg.maTG, tg.HoTenTG,
                       COUNT(ts.maSACH) AS SoSach
                FROM TACGIA tg
                LEFT JOIN TACGIA_SACH ts ON ts.maTG = tg.maTG
                WHERE (:kw IS NULL
                   OR LOWER(tg.HoTenTG) LIKE '%' || LOWER(:kw) || '%')
                GROUP BY tg.maTG, tg.HoTenTG
                ORDER BY tg.maTG";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new TacGiaRow
                {
                    MaTG = r["maTG"].ToString()!,
                    HoTenTG = r["HoTenTG"].ToString()!,
                    SoSach = Convert.ToInt32(r["SoSach"]),
                });
            return list;
        }

        // ═══════════════════════════════════════
        // THỂ LOẠI
        // ═══════════════════════════════════════
        public List<TheLoaiRow> GetTheLoai(string? keyword = null)
        {
            var list = new List<TheLoaiRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
                SELECT tl.MaTL, tl.TenTL,
                       COUNT(tls.maSACH) AS SoSach
                FROM THELOAI tl
                LEFT JOIN THELOAI_SACH tls ON tls.MaTL = tl.MaTL
                WHERE (:kw IS NULL
                   OR LOWER(tl.TenTL) LIKE '%' || LOWER(:kw) || '%')
                GROUP BY tl.MaTL, tl.TenTL
                ORDER BY tl.MaTL";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);
            cmd.Parameters.Add("kw", (object?)keyword ?? DBNull.Value);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new TheLoaiRow
                {
                    MaTL = r["MaTL"].ToString()!,
                    TenTL = r["TenTL"].ToString()!,
                    SoSach = Convert.ToInt32(r["SoSach"]),
                });
            return list;
        }
        // ═══════════════════════════════════════
        // BÁO CÁO SÁCH MƯỢN NHIỀU
        // ═══════════════════════════════════════
        public List<BaoCaoSachMuonRow> GetBaoCaoSachMuon()
        {
            var list = new List<BaoCaoSachMuonRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = "SELECT * FROM V_BAOCAO_SACHMUON";
            using var cmd = new OracleCommand(sql, conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new BaoCaoSachMuonRow
                {
                    MaSach = r["maSACH"].ToString()!,
                    TenSach = r["TenSach"].ToString()!,
                    TenNXB = r["TenNXB"].ToString()!,
                    TacGia = r["TacGia"].ToString()!,
                    TheLoai = r["TheLoai"].ToString()!,
                    TongLuotMuon = Convert.ToInt32(r["TongLuotMuon"]),
                    DangMuon = Convert.ToInt32(r["DangMuon"]),
                    DaTra = Convert.ToInt32(r["DaTra"]),
                });
            return list;
        }

        // ═══════════════════════════════════════
        // XÓA SÁCH
        // ═══════════════════════════════════════
        public (bool Ok, string ThongBao) XoaSach(string maSach)
        {
            using var conn = new OracleConnection(_conn);
            try
            {
                conn.Open();

                using var cmd = new OracleCommand("UPDATE SACH SET DaAn = 1 WHERE maSACH = :maSach", conn);
                cmd.Parameters.Add("maSach", maSach);

                cmd.ExecuteNonQuery();
                return (true, "Xóa (ẩn) sách thành công!");
            }
            catch (OracleException ex)
            {
                if (ex.Number >= 20000 && ex.Number <= 20999)
                {
                    return (false, ex.Message);
                }
                return (false, "Lỗi hệ thống: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════
        // XÓA TÁC GIẢ KHỎI SÁCH — bắt lỗi trigger
        // ═══════════════════════════════════════
        public (bool Ok, string ThongBao) XoaTacGiaKhoiSach(string maTG, string maSach)
        {
            using var conn = new OracleConnection(_conn);
            try
            {
                conn.Open();
                var sql = "DELETE FROM TACGIA_SACH WHERE maTG = :maTG AND maSACH = :maSach";
                using var cmd = new OracleCommand(sql, conn);

                // Truyền tham số
                cmd.Parameters.Add("maTG", maTG);
                cmd.Parameters.Add("maSach", maSach);

                // Khi thực hiện dòng này, nếu vi phạm Trigger, Oracle sẽ ném lỗi ngay lập tức
                int rows = cmd.ExecuteNonQuery();

                if (rows > 0)
                    return (true, "Xóa tác giả thành công!");
                else
                    return (false, "Không tìm thấy dữ liệu để xóa.");
            }
            catch (OracleException ex)
            {
                // ex.Number chính là mã lỗi bạn đặt trong Trigger (ví dụ: 20001)
                if (ex.Number == 20001)
                {
                    // Trả về đúng thông báo lỗi từ Trigger
                    return (false, "Lỗi từ hệ thống: " + GetCleanMessage(ex.Message));
                }
                return (false, "Lỗi Database: " + ex.Message);
            }
        }

        // Hàm bổ trợ để cắt bỏ các ký tự thừa của Oracle (như ORA-20001...)
        private string GetCleanMessage(string rawMessage)
        {
            if (string.IsNullOrEmpty(rawMessage)) return "";
            // Cắt lấy phần nội dung sau dấu hai chấm thứ 2 hoặc dòng đầu tiên
            return rawMessage.Split('\n')[0].Split(':')[1].Trim();
        }

        // Helper
        private static void Execute(OracleConnection conn, OracleTransaction tran, string sql, string param)
        {
            using var cmd = new OracleCommand(sql, conn);
            cmd.Transaction = tran;
            cmd.Parameters.Add("p", param);
            cmd.ExecuteNonQuery();
        }
        // ═══════════════════════════════════════
        // XÓA TÁC GIẢ
        // ═══════════════════════════════════════
        public (bool Ok, string ThongBao) XoaTacGia(string maTG)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                // Xóa liên kết TACGIA_SACH trước
                // Trigger sẽ chặn nếu sách còn tồn tại và đây là tác giả cuối
                var sql1 = "DELETE FROM TACGIA_SACH WHERE maTG = :maTG";
                using (var cmd = new OracleCommand(sql1, conn))
                {
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("maTG", maTG);
                    cmd.ExecuteNonQuery();
                }

                // Xóa tác giả
                var sql2 = "DELETE FROM TACGIA WHERE maTG = :maTG";
                using (var cmd = new OracleCommand(sql2, conn))
                {
                    cmd.Transaction = tran;
                    cmd.Parameters.Add("maTG", maTG);
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                return (true, "Xóa tác giả thành công!");
            }
            catch (OracleException ex)
            {
                tran.Rollback();
                // Bắt lỗi từ trigger -20001
                if (ex.Message.Contains("20001"))
                    return (false, "Không thể xóa! Tác giả này đang liên kết với đầu sách.");
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════
        // THÊM TÁC GIẢ MỚI
        // ═══════════════════════════════════════
        public (bool Ok, string ThongBao) ThemTacGia(string maTG, string hoTenTG)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            try
            {
                var sql = "INSERT INTO TACGIA (maTG, HoTenTG) VALUES (:maTG, :hoTen)";
                using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add("maTG", maTG);
                cmd.Parameters.Add("hoTen", hoTenTG);
                cmd.ExecuteNonQuery();
                return (true, "Thêm tác giả thành công!");
            }
            catch (OracleException ex)
            {
                if (ex.Message.Contains("00001"))
                    return (false, "Mã tác giả đã tồn tại!");
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════
        // SỬA TÊN TÁC GIẢ
        // ═══════════════════════════════════════
        public (bool Ok, string ThongBao) SuaTacGia(string maTG, string hoTenTG)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            try
            {
                var sql = "UPDATE TACGIA SET HoTenTG = :hoTen WHERE maTG = :maTG";
                using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add("hoTen", hoTenTG);
                cmd.Parameters.Add("maTG", maTG);
                cmd.ExecuteNonQuery();
                return (true, "Cập nhật tác giả thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }
    }
}