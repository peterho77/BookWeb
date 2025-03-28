﻿using Book.DataAccess.Data;
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
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
			List<Product> product = _unitOfWork.product.GetAll(includeProperties:"category").ToList();

			return View(product);
        }

        public IActionResult Upsert(int? id)
        {
            //ViewBag.CategoryList = list;
            //ViewData["CategoryList"] = list;

            ProductVM productVM = new ProductVM()
            {
                CategoryList = _unitOfWork.category.GetAll().Select(u => new SelectListItem
				{
					Text = u.Name,
					Value = u.Id.ToString()
				}),
				Product = new Product()
            };
            if (id == 0 || id == null)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.product.Get(u => u.Id == id);
                return View(productVM);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM,IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwrootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwrootPath, @"images\product");

                    if(!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwrootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName),FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    productVM.Product.ImageUrl = @"\images\product\" + fileName; 
                }

                if(productVM.Product.Id == 0)
                {
                    _unitOfWork.product.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork.product.Update(productVM.Product);
                }
                TempData["success"] = "Product created successfully.";
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            return View();
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> productList = _unitOfWork.product.GetAll(includeProperties: "category").ToList();

            return Json(new { data = productList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product? product = _unitOfWork.product.Get(obj => obj.Id == id);
            if (product == null)
            {
                return Json(new { icon = "error", message = "Error while deleting" });
            }
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }
            _unitOfWork.product.Remove(product);
            _unitOfWork.Save();
            return Json(new { icon = "success", message = "Delete sucessfully" });
        }

        #endregion

    }
}



