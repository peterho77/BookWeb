using Book.DataAccess.Repository;
using Book.DataAccess.Repository.IRepository;
using Book.Models;
using Book.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BookWeb.Areas.Customer.Controllers
{
	[Area("Customer")]
	public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger,IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {

            IEnumerable<Product> productList = _unitOfWork.product.GetAll().ToList();
            return View(productList);
        }

		public IActionResult Details(int productId)
		{
            ShoppingCart cart = new ShoppingCart()
            {
                Product = _unitOfWork.product.Get(u => u.Id == productId, includeProperties: "category"),
                Count = 1,
                ProductId = productId
            };
			return View(cart);
		}
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cart.ApplicationUserId = userId;
            
            ShoppingCart cartFromDb = _unitOfWork.shoppingCart.Get(u => u.ProductId == cart.ProductId && u.ApplicationUserId == cart.ApplicationUserId);
            if (cartFromDb == null)
            {
                // Add new shopping cart
                _unitOfWork.shoppingCart.Add(cart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId).Count());

            }
            else
            {
                // Update count in  exist shopping cart
                cartFromDb.Count += cart.Count;
                _unitOfWork.shoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }

           
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
