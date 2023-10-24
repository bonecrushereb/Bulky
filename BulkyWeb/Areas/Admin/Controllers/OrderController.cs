using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("admin")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int orderId)
        {
               OrderVM = new() {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromdb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromdb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromdb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromdb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromdb.City = OrderVM.OrderHeader.City;
            orderHeaderFromdb.State = OrderVM.OrderHeader.State;
            orderHeaderFromdb.PostalCode = OrderVM.OrderHeader.PostalCode;

            if(!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier)) {
                orderHeaderFromdb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if(!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber)) {
                orderHeaderFromdb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromdb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully";
            

            return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromdb.Id});
        }

    #region API Calls

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;
            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee)) {
                 objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            } else {

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                objOrderHeaders =  _unitOfWork.OrderHeader
                    .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            switch(status) {
                case "pending":
                objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusPending);
                break; 
                case "inprocess":
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                break; 
                case "completed":
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                break;
                case "approved":
                objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusApproved);
                break; 
                default:
                break;
            }

            return Json(new { data = objOrderHeaders });
        }

        #endregion
    }
}