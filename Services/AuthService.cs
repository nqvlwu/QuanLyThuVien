using Oracle.ManagedDataAccess.Client;
using LibraryOS.Models;

namespace LibraryOS.Services
{
    public class AuthService
    {
        private readonly string _conn;

        public AuthService(IConfiguration config)
        {
            _conn = config.GetConnectionString("OracleDbConnection")!;
        }

        public AppUser? Authenticate(string username, string password)
        {
            using var conn = new OracleConnection(_conn);
            conn.Open();

            // LoaiND: '1'=Admin, '2'=Thủ thư, '3'=Đọc giả
            var sql = @"
                SELECT 
                    tk.TenDN,
                    tk.LoaiND,
                    nv.HoTenNV,
                    ttv.HoTenDG,
                    tk.MaNV,
                    tk.SoTheTV
                FROM TAIKHOAN tk
                LEFT JOIN NHANVIEN   nv  ON tk.MaNV    = nv.MaNV
                LEFT JOIN THETHUVIEN ttv ON tk.SoTheTV = ttv.SoTheTV
                WHERE tk.TenDN   = :username
                  AND tk.MatKhau = :password
                  AND ROWNUM     = 1";

            using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add("username", username);
            cmd.Parameters.Add("password", password);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var loaiND = reader["LoaiND"]?.ToString() ?? "";

            var (role, dbRole, fullName) = loaiND switch
            {
                "1" => ("ql", "ADMIN_ROLE", reader["HoTenNV"]?.ToString() ?? username),
                "2" => ("tt", "THUTHU_ROLE", reader["HoTenNV"]?.ToString() ?? username),
                "3" => ("dg", "DOCGIA_ROLE", reader["HoTenDG"]?.ToString() ?? username),
                _ => ("dg", "DOCGIA_ROLE", username)
            };

            return new AppUser
            {
                Username = username,
                FullName = fullName,
                Role = role,
                DbRole = dbRole,
                MaNV = reader["MaNV"]?.ToString() ?? "",
                SoTheTV = reader["SoTheTV"]?.ToString() ?? ""
            };
        }
    }
}