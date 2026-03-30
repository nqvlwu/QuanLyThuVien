using LibraryOS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryOS.Controllers
{
    [Authorize(Roles = "DOCGIA_ROLE")]
    public class DocGiaController : Controller
    {
        private readonly DashboardService _svc;
        public DocGiaController(DashboardService svc) { _svc = svc; }

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Trang chủ";
            ViewData["ActiveMenu"] = "dashboard";
            var soTheTV = User.FindFirstValue("SoTheTV") ?? "";
            var vm = _svc.GetDocGiaDashboard(soTheTV);
            return View(vm);
        }
        public IActionResult TimSach() { ViewData["Title"] = "Tìm kiếm sách"; ViewData["ActiveMenu"] = "sach"; return View(); }
        public IActionResult TheLoai() { ViewData["Title"] = "Theo thể loại"; ViewData["ActiveMenu"] = "theloai"; return View(); }
        public IActionResult TacGia() { ViewData["Title"] = "Theo tác giả"; ViewData["ActiveMenu"] = "tacgia"; return View(); }
        public IActionResult SachDangMuon() { ViewData["Title"] = "Sách đang mượn"; ViewData["ActiveMenu"] = "phieumuon"; return View(); }
        public IActionResult TheTV() { ViewData["Title"] = "Thẻ thư viện"; ViewData["ActiveMenu"] = "thethuvien"; return View(); }
    }
}