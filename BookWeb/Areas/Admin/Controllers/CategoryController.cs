using Book.DataAccess.Data;
using Book.DataAccess.Repository;
using Book.DataAccess.Repository.IRepository;
using Book.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _db;
        public CategoryController(IUnitOfWork db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            List<Category> category = _db.category.GetAll().ToList();
            return View(category);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (category.Name.ToLower() == category.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "Category name and display order must not be the same");
            }
            if (ModelState.IsValid)
            {
                TempData["success"] = "Category created successfully.";
                _db.category.Add(category);
                _db.Save();
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Category? category = _db.category.Get(obj => obj.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        [HttpPost]
        public IActionResult Edit(Category? category)
        {
            if (category == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                TempData["success"] = "Category updated successfully.";
                _db.category.Update(category);
                _db.Save();
                return RedirectToAction("Index");
            }
            return NotFound();
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Category? category = _db.category.Get(obj => obj.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteCategory(Category? category)
        {
            if (category == null)
            {
                return NotFound();
            }
            TempData["danger"] = "Category deleted successfully.";
            _db.category.Remove(category);
            _db.Save();
            return RedirectToAction("Index");
        }
    }
}
