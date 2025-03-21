using Book.DataAccess.Repository.IRepository;
using Book.Models;
using Book.Models.ViewModels;
using Book.Models.Vnpay;
using Book.Models.Vnpay.Services;
using Book.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace BookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IVnPayService _vnPayService;
		[BindProperty]
        public ShoppingCartVM ShoppingCartVM {  get; set; }
        public CartController(IUnitOfWork unitOfWork, IVnPayService vnPayService)
        {
            _unitOfWork = unitOfWork;
            _vnPayService = vnPayService;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach(var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAdress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAdress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;

            foreach (var cart in ShoppingCartVM.ShoppingCartList) 
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
            }

            return View(ShoppingCartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

			ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser user = _unitOfWork.applicationUser.Get(u => u.Id == userId);

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
			}

            if(user.CompanyId.GetValueOrDefault() == 0)
            {
				//it is a regular customer account
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
            {
				//company account
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment ;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
            _unitOfWork.orderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach(var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
				_unitOfWork.orderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}

			if (user.CompanyId.GetValueOrDefault() == 0)
			{
				//it is a regular customer account
                //stripe logic
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}

			return View(ShoppingCartVM);
		}

        public IActionResult OrderConfirmation(ShoppingCartVM shoppingCartVM)
        {

			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			shoppingCartVM.ShoppingCartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
			}

            PaymentInformationModel model = new()
            {
                Amount = shoppingCartVM.OrderHeader.OrderTotal,
                Name = shoppingCartVM.OrderHeader.Name
            };

			return View(model);
        }

		//Thanh toán Vnpay
		public IActionResult CreatePaymentUrl(PaymentInformationModel model)
		{
			var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

			return Redirect(url);
		}

		public IActionResult PaymentCallback()
		{
			var response = _vnPayService.PaymentExecute(Request.Query);

			return Json(response);
		}

        //

		public IActionResult plus(int cartId)
        {
            ShoppingCart cartFromDb = _unitOfWork.shoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count += 1; 
            _unitOfWork.shoppingCart.Update(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult minus(int cartId)
        {
            ShoppingCart cartFromDb = _unitOfWork.shoppingCart.Get(u => u.Id == cartId);
            if(cartFromDb.Count <= 1)
            {
                _unitOfWork.shoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.shoppingCart.Update(cartFromDb);
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int cartId)
        {
            ShoppingCart cartFromDb = _unitOfWork.shoppingCart.Get(u => u.Id == cartId);
            _unitOfWork.shoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        //Thanh toán VnPay
        public IActionResult VnpayPaymentConfirm()
        {

            return View();
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)   
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            else
            {
                return shoppingCart.Product.Price100;
            }
        }
    }
}
    