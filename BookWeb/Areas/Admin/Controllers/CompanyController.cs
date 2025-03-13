using Book.DataAccess.Data;
using Book.DataAccess.Repository;
using Book.DataAccess.Repository.IRepository;
using Book.Models;
using Book.Models.ViewModels;
using Book.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public CompanyController(IUnitOfWork db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
			List<Company> company = _db.company.GetAll().ToList();

			return View(company);
        }

        public IActionResult Upsert(int? id)
        {
            //ViewBag.CategoryList = list;
            //ViewData["CategoryList"] = list;

            if (id == 0 || id == null)
            {
                return View(new Company());
            }
            else
            {
                Company company= _db.company.Get(u => u.Id == id);
                return View(company);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {

                if(company.Id == 0)
                {
                    _db.company.Add(company);
                }
                else
                {
                    _db.company.Update(company);
                }
                TempData["success"] = "Company created successfully.";
                _db.Save();
                return RedirectToAction("Index");
            }
            return View();
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> companyList = _db.company.GetAll().ToList();

            return Json(new { data = companyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Company? company = _db.company.Get(obj => obj.Id == id);
            if (company == null)
            {
                return Json(new { icon = "error", message = "Error while deleting" });
            }
            _db.company.Remove(company);
            _db.Save();
            return Json(new { icon = "success", message = "Delete sucessfully" });
        }

        #endregion

    }
}



