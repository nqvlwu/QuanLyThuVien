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
        private readonly DashboardService _dashboard;
        private readonly PhieuMuonService _phieuMuon;
        private readonly QuanLyService _ql;

        public QuanLyController(DashboardService dashboard, PhieuMuonService phieuMuon, QuanLyService ql)
        {
            _dashboard = dashboard;
            _phieuMuon = phieuMuon;
            _ql = ql;
        }

        public IActionResult NhanVien(string? kw = null)
        {
            ViewData["Title"] = "Nhân viên";
            ViewData["ActiveMenu"] = "nhanvien";
            ViewBag.Keyword = kw;
            return View(_ql.GetNhanVien(kw));
        }

        public IActionResult Sach(string? kw = null, string? maTL = null)
        {
            ViewData["Title"] = "Sách & Kho";
            ViewData["ActiveMenu"] = "sach";
            ViewBag.Keyword = kw;
            ViewBag.MaTL = maTL;
            ViewBag.DsTheLoai = _ql.GetTheLoai();
            return View(_ql.GetSach(kw, maTL));
        }

        public IActionResult NhaXuatBan(string? kw = null)
        {
            ViewData["Title"] = "Nhà xuất bản";
            ViewData["ActiveMenu"] = "nxb";
            ViewBag.Keyword = kw;
            return View(_ql.GetNhaXuatBan(kw));
        }

        public IActionResult TacGia(string? kw = null)
        {
            ViewData["Title"] = "Tác giả";
            ViewData["ActiveMenu"] = "tacgia";
            ViewBag.Keyword = kw;
            return View(_ql.GetTacGia(kw));
        }

        public IActionResult TheLoai(string? kw = null)
        {
            ViewData["Title"] = "Thể loại";
            ViewData["ActiveMenu"] = "theloai";
            ViewBag.Keyword = kw;
            return View(_ql.GetTheLoai(kw));
        }
        public IActionResult PhieuMuon(string? tinhTrang = null)
        {
            ViewData["Title"] = "Phiếu mượn";
            ViewData["ActiveMenu"] = "phieumuon";
            var ds = _phieuMuon.GetDanhSachPhieuMuon(tinhTrang);
            ViewBag.TinhTrang = tinhTrang;
            return View("~/Views/ThuThu/PhieuMuon.cshtml", ds); // dùng chung View
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
            return View("~/Views/ThuThu/TaoPhieuMuon.cshtml", vm);
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
            var vm = _dashboard.GetQuanLyDashboard();
            return View(vm);
        }
        // Báo cáo sách mượn nhiều
        public IActionResult ThongKe()
        {
            ViewData["Title"] = "Thống kê mượn";
            ViewData["ActiveMenu"] = "thongke";
            return View(_ql.GetBaoCaoSachMuon());
        }

        // Xóa sách (gọi từ AJAX)
        [HttpPost]
        public IActionResult XoaSach(string maSach)
        {
            var (ok, msg) = _ql.XoaSach(maSach);
            return Json(new { ok, msg });
        }
        // Lấy tác giả của sách (AJAX)
        [HttpGet]
        public IActionResult GetTacGiaCuaSach(string maSach)
        {
            var list = _ql.GetTacGiaCuaSach(maSach);
            var dsTacGia = _ql.GetTacGia(); // tất cả tác giả để chọn thêm
            return Json(new
            {
                tacGiaCuaSach = list.Select(t => new { t.MaTG, t.HoTenTG }),
                tatCaTacGia = dsTacGia.Select(t => new { t.MaTG, t.HoTenTG })
            });
        }

        // Thêm tác giả vào sách (AJAX)
        [HttpPost]
        public IActionResult ThemTacGiaVaoSach(string maTG, string maSach)
        {
            var (ok, msg) = _ql.ThemTacGiaVaoSach(maTG, maSach);
            return Json(new { ok, msg });
        }

        [HttpPost]
        public IActionResult XoaTacGia(string maTG)
        {
            var (ok, msg) = _ql.XoaTacGia(maTG);
            return Json(new { ok, msg });
        }

        [HttpPost]
        public IActionResult ThemTacGia(string maTG, string hoTenTG)
        {
            var (ok, msg) = _ql.ThemTacGia(maTG, hoTenTG);
            return Json(new { ok, msg });
        }

        [HttpPost]
        public IActionResult SuaTacGia(string maTG, string hoTenTG)
        {
            var (ok, msg) = _ql.SuaTacGia(maTG, hoTenTG);
            return Json(new { ok, msg });
        }
    }
}