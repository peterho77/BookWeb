using Book.DataAccess.Migrations;
using Book.DataAccess.Repository.IRepository;
using Book.Models;
using Book.Models.ViewModels;
using Book.Models.Vnpay;
using Book.Models.Vnpay.Services;
using Book.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stripe.Checkout;
using System.Collections;
using System.Reflection;
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
                OrderHeader = new(),
                PaymentMethodList = typeof(SD).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                       .Where(f => f.Name.StartsWith("PaymentMethod_"))
                                       .Select(f => new SelectListItem
									   {
										   Text = f.GetValue(null).ToString(), // Hiển thị giá trị hằng số
										   Value = f.Name // Sử dụng tên hằng số làm giá trị
									   })
									   .ToList()
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
                OrderHeader = new(),

                PaymentMethodList = typeof(SD).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                       .Where(f => f.Name.StartsWith("PaymentMethod_"))
                                       .Select(f => new SelectListItem
                                       {
                                           Text = f.GetValue(null).ToString(), // Hiển thị giá trị hằng số
                                           Value = f.GetValue(null).ToString() // Sử dụng tên hằng số làm giá trị
									   })
                                       .ToList()
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

            HttpContext.Session.SetInt32("orderHeaderId", ShoppingCartVM.OrderHeader.Id);

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
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

            //Thanh toán stripe
            if (user.CompanyId.GetValueOrDefault() == 0 && ShoppingCartVM.OrderHeader.PaymentMethod == SD.PaymentMethod_Stripe)
            {
                //it is a regular customer account
                //stripe logic

                var domain = "https://localhost:7166/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + "customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in ShoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);
                _unitOfWork.orderHeader.UpdatePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            else if(user.CompanyId.GetValueOrDefault() == 0 &&  ShoppingCartVM.OrderHeader.PaymentMethod == SD.PaymentMethod_Vnpay)
            {
                

                return RedirectToAction(nameof(CreatePaymentUrl), new PaymentInformationModel {
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Name = ShoppingCartVM.OrderHeader.Name,
                    Amount = Convert.ToDouble(ShoppingCartVM.OrderHeader.OrderTotal * 22000),
                    OrderType = "Book Order",
                    OrderDescription = $"chuyển khoản {SD.PaymentMethod_Vnpay}"
                });
            }
            else if(user.CompanyId.GetValueOrDefault() != 0)
            {

                TempData["success"] = "You ordered successfully. You have 30 days to get payment done after order shipped";

				List<ShoppingCart> shoppingCarts = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId).ToList();
				_unitOfWork.shoppingCart.RemoveRange(shoppingCarts);
				_unitOfWork.Save();

				HttpContext.Session.Clear();


                return RedirectToAction("Details", "Order", new { area = "Admin"  ,orderId = ShoppingCartVM.OrderHeader.Id});
			}

            return View(ShoppingCartVM);

		}

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.orderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                if(orderHeader.PaymentStatus == SD.PaymentMethod_Stripe)
                {
                    //this is an order by customer
                    var service = new SessionService();
                    Session session = service.Get(orderHeader.SessionId);

                    if (session.PaymentStatus.ToLower() == "paid")
                    {
                        _unitOfWork.orderHeader.UpdatePaymentId(orderHeader.Id, session.PaymentIntentId, session.Id);
                        _unitOfWork.orderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusApproved);
                        _unitOfWork.Save();
                    }
                }
                else if (orderHeader.PaymentStatus == SD.PaymentMethod_Vnpay)
                {
                    if (HttpContext.Session.GetString("vnpay_response_success") == "true")
                    {
                        HttpContext.Session.Clear();
                        _unitOfWork.orderHeader.UpdatePaymentId(orderHeader.Id, HttpContext.Session.GetString("vnpay_response_paymentId"), null);
                        _unitOfWork.orderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusApproved);
                        _unitOfWork.Save();
                    }
				}
            }

            List<ShoppingCart> shoppingCarts = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _unitOfWork.shoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

			HttpContext.Session.Clear();

            OrderVM order = new()
            {
                OrderDetails = _unitOfWork.orderDetail.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product"),
                OrderHeader = _unitOfWork.orderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser")
            };

            return View(order);
        }
        [HttpPost]
        public IActionResult OrderConfirmation(OrderVM order)
        {
            return View(order);
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

            HttpContext.Session.SetString("vnpay_response_paymentId", response.PaymentId);
            HttpContext.Session.SetString("vnpay_response_success", response.Success.ToString());

            int? id = HttpContext.Session.GetInt32("orderHeaderId");

            return RedirectToAction(nameof(OrderConfirmation), new {id = id});

        }

        // Nút thêm trừ và xóa trong giỏ hàng

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
            ShoppingCart cartFromDb = _unitOfWork.shoppingCart.Get(u => u.Id == cartId, tracked: true);
            if(cartFromDb.Count <= 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
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
            ShoppingCart cartFromDb = _unitOfWork.shoppingCart.Get(u => u.Id == cartId, tracked : true);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            _unitOfWork.shoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
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
    