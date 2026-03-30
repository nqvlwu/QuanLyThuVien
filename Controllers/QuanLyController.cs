using LibraryOS.Models;
using LibraryOS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryOS.Controllers
{
    [Authorize(Roles = "ADMIN_ROLE")]
    public class QuanLyController : Controller
    {
        private readonly DashboardService _svc;
        private readonly PhieuMuonService _phieuMuon;

        public QuanLyController(DashboardService svc, PhieuMuonService phieuMuon)
        {
            _svc = svc;
            _phieuMuon = phieuMuon;
        }

        [HttpGet]
        public IActionResult TaoPhieuMuon()
        {
            ViewData["Title"] = "Tạo phiếu mượn";
            ViewData["ActiveMenu"] = "phieumuon";
            var vm = new TaoPhieuMuonVM
            {
                NgayMuon = DateTime.Now.ToString("yyyy-MM-dd"),
                NgayTra = DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"),
                DanhSachSach = _phieuMuon.GetSachCoThe()
            };
            return View("~/Views/ThuThu/TaoPhieuMuon.cshtml", vm); // dùng chung View
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoPhieuMuon(TaoPhieuMuonVM model)
        {
            ViewData["Title"] = "Tạo phiếu mượn";
            ViewData["ActiveMenu"] = "phieumuon";
            model.DanhSachSach = _phieuMuon.GetSachCoThe();
            foreach (var cs in model.DanhSachSach)
                cs.DuocChon = model.DanhSachCuonSach.Contains(cs.MaCuonSach);

            var (hoTen, hopLe) = _phieuMuon.KiemTraThe(model.SoTheTV);
            model.HoTenDG = hoTen;
            model.TheHopLe = hopLe;

            if (!hopLe)
            {
                model.ThongBao = hoTen == "Không tìm thấy thẻ"
                    ? "Không tìm thấy thẻ thư viện này."
                    : "Thẻ thư viện đã hết hạn.";
                return View("~/Views/ThuThu/TaoPhieuMuon.cshtml", model);
            }

            var maNV = User.FindFirstValue("MaNV") ?? "";
            var ngayMuon = DateTime.Parse(model.NgayMuon);
            var ngayTra = DateTime.Parse(model.NgayTra);

            var (ok, thongBao) = _phieuMuon.TaoPhieuMuon(
                model.SoTheTV, maNV, ngayMuon, ngayTra,
                model.DanhSachCuonSach, model.GhiChu);

            model.ThongBao = thongBao;
            model.ThanhCong = ok;

            if (ok)
            {
                model.SoTheTV = "";
                model.HoTenDG = "";
                model.GhiChu = "";
                model.DanhSachCuonSach = new();
                model.NgayMuon = DateTime.Now.ToString("yyyy-MM-dd");
                model.NgayTra = DateTime.Now.AddDays(14).ToString("yyyy-MM-dd");
                model.DanhSachSach = _phieuMuon.GetSachCoThe();
            }

            return View("~/Views/ThuThu/TaoPhieuMuon.cshtml", model);
        }

        [HttpGet]
        public IActionResult KiemTraThe(string soTheTV)
        {
            var (hoTen, hopLe) = _phieuMuon.KiemTraThe(soTheTV);
            return Json(new { hoTen, hopLe });
        }
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["ActiveMenu"] = "dashboard";
            var vm = _svc.GetQuanLyDashboard();
            return View(vm);
        }
        public IActionResult NhanVien() { ViewData["Title"] = "Nhân viên"; ViewData["ActiveMenu"] = "nhanvien"; return View(); }
        public IActionResult Sach() { ViewData["Title"] = "Sách & Kho"; ViewData["ActiveMenu"] = "sach"; return View(); }
        public IActionResult NhaXuatBan() { ViewData["Title"] = "Nhà xuất bản"; ViewData["ActiveMenu"] = "nxb"; return View(); }
        public IActionResult TacGia() { ViewData["Title"] = "Tác giả"; ViewData["ActiveMenu"] = "tacgia"; return View(); }
        public IActionResult TheLoai() { ViewData["Title"] = "Thể loại"; ViewData["ActiveMenu"] = "theloai"; return View(); }
        public IActionResult ThongKe() { ViewData["Title"] = "Thống kê"; ViewData["ActiveMenu"] = "thongke"; return View(); }
        public IActionResult PhieuPhat() { ViewData["Title"] = "Phiếu phạt"; ViewData["ActiveMenu"] = "phieuphat"; return View(); }
        public IActionResult PhanQuyen() { ViewData["Title"] = "Phân quyền DB"; ViewData["ActiveMenu"] = "phanquyen"; return View(); }
    }
}