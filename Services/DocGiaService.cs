using Oracle.ManagedDataAccess.Client;
using LibraryOS.Models;

namespace LibraryOS.Services
{
    public class DocGiaService
    {
        private readonly string _conn;
        public DocGiaService(IConfiguration config)
        {
            _conn = config.GetConnectionString("OracleDbConnection")!;
        }

        // ═══════════════════════════════════════
        // TÌM KIẾM SÁCH
        // ═══════════════════════════════════════
        public List<SachRow> TimSach(string? kw = null, string? maTL = null, string? maTG = null)
        {
            var list = new List<SachRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();

            var sql = @"
                SELECT s.maSACH, s.TenSach, s.GiaSach, s.NamXB,
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
                   OR LOWER(s.TenSach) LIKE '%'||LOWER(:kw)||'%'
                   OR LOWER(s.maSACH)  LIKE '%'||LOWER(:kw)||'%')
                AND (:maTL IS NULL OR EXISTS (
                    SELECT 1 FROM THELOAI_SACH tls
                    WHERE tls.maSACH = s.maSACH AND tls.MaTL = :maTL))
                AND (:maTG IS NULL OR EXISTS (
                    SELECT 1 FROM TACGIA_SACH ts
                    WHERE ts.maSACH = s.maSACH AND ts.maTG = :maTG))
                ORDER BY s.TenSach";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("kw", (object?)kw ?? DBNull.Value);
            cmd.Parameters.Add("kw", (object?)kw ?? DBNull.Value);
            cmd.Parameters.Add("maTL", (object?)maTL ?? DBNull.Value);
            cmd.Parameters.Add("maTL", (object?)maTL ?? DBNull.Value);
            cmd.Parameters.Add("maTG", (object?)maTG ?? DBNull.Value);
            cmd.Parameters.Add("maTG", (object?)maTG ?? DBNull.Value);

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
                });
            return list;
        }

        // ═══════════════════════════════════════
        // THỂ LOẠI
        // ═══════════════════════════════════════
        public List<TheLoaiRow> GetTheLoai()
        {
            var list = new List<TheLoaiRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();
            var sql = @"
                SELECT tl.MaTL, tl.TenTL, COUNT(tls.maSACH) AS SoSach
                FROM THELOAI tl
                LEFT JOIN THELOAI_SACH tls ON tls.MaTL = tl.MaTL
                GROUP BY tl.MaTL, tl.TenTL
                ORDER BY tl.MaTL";
            using var cmd = new OracleCommand(sql, conn);
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
        // TÁC GIẢ
        // ═══════════════════════════════════════
        public List<TacGiaRow> GetTacGia()
        {
            var list = new List<TacGiaRow>();
            using var conn = new OracleConnection(_conn);
            conn.Open();
            var sql = @"
                SELECT tg.maTG, tg.HoTenTG, COUNT(ts.maSACH) AS SoSach
                FROM TACGIA tg
                LEFT JOIN TACGIA_SACH ts ON ts.maTG = tg.maTG
                GROUP BY tg.maTG, tg.HoTenTG
                ORDER BY tg.HoTenTG";
            using var cmd = new OracleCommand(sql, conn);
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
        // THẺ THƯ VIỆN CỦA ĐỘC GIẢ
        // ═══════════════════════════════════════
        public TheTV? GetTheTV(string soTheTV)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();
            var sql = @"
                SELECT SoTheTV, HoTenDG, GioiTinhDG,
                       TO_CHAR(NgaySinhDG,'DD/MM/YYYY')  AS NgaySinh,
                       DiaChiDG, GhiChu,
                       TO_CHAR(NgayBatDau,'DD/MM/YYYY')  AS NgayBatDau,
                       TO_CHAR(NgayHetHan,'DD/MM/YYYY')  AS NgayHetHan,
                       CASE WHEN NgayHetHan >= SYSDATE THEN 1 ELSE 0 END AS ConHan,
                       ROUND(NgayHetHan - SYSDATE) AS NgayConLai
                FROM THETHUVIEN WHERE SoTheTV = :soTheTV";
            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("soTheTV", soTheTV);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new TheTV
            {
                SoTheTV = r["SoTheTV"].ToString()!,
                HoTenDG = r["HoTenDG"].ToString()!,
                GioiTinh = r["GioiTinhDG"].ToString()!,
                NgaySinh = r["NgaySinh"].ToString()!,
                DiaChi = r["DiaChiDG"].ToString()!,
                GhiChu = r["GhiChu"].ToString()!,
                NgayBatDau = r["NgayBatDau"].ToString()!,
                NgayHetHan = r["NgayHetHan"].ToString()!,
                ConHan = Convert.ToInt32(r["ConHan"]) == 1,
                NgayConLai = r["NgayConLai"] == DBNull.Value ? 0 : Convert.ToInt32(r["NgayConLai"]),
            };
        }
    }
}