using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Implementations;
using Do_An_E_Commerce_BHX.Services.Interfaces;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Controllers
{
    [Authorize]
    public class UserAddressController : BaseController
    {
        private readonly IUserAddressService _userAddressService;

        public UserAddressController()
        {
            _userAddressService = new UserAddressService(DbContext);
        }

        public UserAddressController(IUserAddressService userAddressService, ApplicationDbContext dbContext) : base(dbContext)
        {
            _userAddressService = userAddressService ?? new UserAddressService(DbContext);
        }

        // GET: UserAddress
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            var addresses = await _userAddressService.GetUserAddressesAsync(userId);
            return View(addresses);
        }

        // GET: UserAddress/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserAddress/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserAddress model)
        {
            var userId = User.Identity.GetUserId();
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                await _userAddressService.CreateAddressAsync(model, userId);
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // GET: UserAddress/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var userId = User.Identity.GetUserId();
            var address = await _userAddressService.GetUserAddressByIdAsync(id.Value, userId);

            if (address == null) return HttpNotFound();

            return View(address);
        }

        // POST: UserAddress/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(UserAddress model)
        {
            var userId = User.Identity.GetUserId();
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                bool success = await _userAddressService.UpdateAddressAsync(model, userId);
                if (!success) return HttpNotFound();

                return RedirectToAction("Index");
            }

            return View(model);
        }

        // POST: UserAddress/SetDefault/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetDefault(int id)
        {
            var userId = User.Identity.GetUserId();
            await _userAddressService.SetDefaultAddressAsync(id, userId);
            return RedirectToAction("Index");
        }

        // POST: UserAddress/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = User.Identity.GetUserId();
            await _userAddressService.DeleteAddressAsync(id, userId);
            return RedirectToAction("Index");
        }
    }
}