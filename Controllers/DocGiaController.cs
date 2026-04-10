using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LibraryOS.Services;

namespace LibraryOS.Controllers
{
    [Authorize(Roles = "DOCGIA_ROLE")]
    public class DocGiaController : Controller
    {
        private readonly DashboardService _dashboard;
        private readonly DocGiaService _dg;

        public DocGiaController(DashboardService dashboard, DocGiaService dg)
        {
            _dashboard = dashboard;
            _dg = dg;
        }

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Trang chủ";
            ViewData["ActiveMenu"] = "dashboard";
            var soTheTV = User.FindFirstValue("SoTheTV") ?? "";
            var vm = _dashboard.GetDocGiaDashboard(soTheTV);
            return View(vm);
        }

        public IActionResult TimSach(string? kw = null, string? maTL = null)
        {
            ViewData["Title"] = "Tìm kiếm sách";
            ViewData["ActiveMenu"] = "sach";
            ViewBag.Keyword = kw ?? "";
            ViewBag.MaTL = maTL ?? "";
            ViewBag.DsTheLoai = _dg.GetTheLoai();
            return View(_dg.TimSach(kw, maTL));
        }

        public IActionResult TheLoai(string? maTL = null)
        {
            ViewData["Title"] = "Theo thể loại";
            ViewData["ActiveMenu"] = "theloai";
            ViewBag.MaTL = maTL ?? "";
            ViewBag.DsTheLoai = _dg.GetTheLoai();
            var sach = maTL != null ? _dg.TimSach(maTL: maTL) : new();
            return View(sach);
        }

        public IActionResult TacGia(string? maTG = null)
        {
            ViewData["Title"] = "Theo tác giả";
            ViewData["ActiveMenu"] = "tacgia";
            ViewBag.MaTG = maTG ?? "";
            ViewBag.DsTacGia = _dg.GetTacGia();
            var sach = maTG != null ? _dg.TimSach(maTG: maTG) : new();
            return View(sach);
        }

        public IActionResult SachDangMuon()
        {
            ViewData["Title"] = "Sách đang mượn";
            ViewData["ActiveMenu"] = "phieumuon";
            var soTheTV = User.FindFirstValue("SoTheTV") ?? "";
            var vm = _dashboard.GetDocGiaDashboard(soTheTV);
            return View(vm);
        }

        public IActionResult TheTV()
        {
            ViewData["Title"] = "Thẻ thư viện";
            ViewData["ActiveMenu"] = "thethuvien";
            var soTheTV = User.FindFirstValue("SoTheTV") ?? "";
            var the = _dg.GetTheTV(soTheTV);
            return View(the);
        }
    }
}