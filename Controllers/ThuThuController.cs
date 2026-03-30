using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LibraryOS.Services;
using LibraryOS.Models;

namespace LibraryOS.Controllers
{
    [Authorize(Roles = "THUTHU_ROLE")]
    public class ThuThuController : Controller
    {
        private readonly DashboardService _svc;
        private readonly PhieuMuonService _phieuMuon;

        public ThuThuController(DashboardService svc, PhieuMuonService phieuMuon)
        {
            _svc = svc;
            _phieuMuon = phieuMuon;
        }

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["ActiveMenu"] = "dashboard";
            var vm = _svc.GetThuThuDashboard();
            return View(vm);
        }

        [HttpGet]
        public IActionResult TaoPhieuMuon()
        {
            ViewData["Title"] = "Tạo phiếu mượn";
            ViewData["ActiveMenu"] = "taophieumuon";
            var vm = new TaoPhieuMuonVM
            {
                NgayMuon = DateTime.Now.ToString("yyyy-MM-dd"),
                NgayTra = DateTime.Now.AddDays(14).ToString("yyyy-MM-dd"),
                DanhSachSach = _phieuMuon.GetSachCoThe()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoPhieuMuon(TaoPhieuMuonVM model)
        {
            ViewData["Title"] = "Tạo phiếu mượn";
            ViewData["ActiveMenu"] = "taophieumuon";
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
                return View(model);
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

            return View(model);
        }

        [HttpGet]
        public IActionResult KiemTraThe(string soTheTV)
        {
            var (hoTen, hopLe) = _phieuMuon.KiemTraThe(soTheTV);
            return Json(new { hoTen, hopLe });
        }

        public IActionResult PhieuMuon(string? tinhTrang = null)
        {
            ViewData["Title"] = "Phiếu mượn";
            ViewData["ActiveMenu"] = "phieumuon";
            var ds = _phieuMuon.GetDanhSachPhieuMuon(tinhTrang);
            ViewBag.TinhTrang = tinhTrang;
            return View(ds);
        }

        public IActionResult PhieuNhap() { ViewData["Title"] = "Phiếu nhập"; ViewData["ActiveMenu"] = "phieunhap"; return View(); }
        public IActionResult TheTV() { ViewData["Title"] = "Thẻ thư viện"; ViewData["ActiveMenu"] = "thethuvien"; return View(); }
        public IActionResult PhieuPhat() { ViewData["Title"] = "Phiếu phạt"; ViewData["ActiveMenu"] = "phieuphat"; return View(); }
        public IActionResult Sach() { ViewData["Title"] = "Danh mục sách"; ViewData["ActiveMenu"] = "sach"; return View(); }
        public IActionResult CuonSach() { ViewData["Title"] = "Cuốn sách"; ViewData["ActiveMenu"] = "cuonsach"; return View(); }
    }
}