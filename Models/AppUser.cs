namespace LibraryOS.Models
{
    public class AppUser
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public string DbRole { get; set; } = "";
        public string MaNV { get; set; } = "";   // dùng cho NV
        public string SoTheTV { get; set; } = "";   // dùng cho Đọc giả
    }
}