using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Microsoft.AspNet.Identity;

namespace Do_An_E_Commerce_BHX.Controllers
{
    [Authorize]
    public class UserAddressController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: UserAddress
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            var addresses = await db.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

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

            // QUAN TRỌNG: Gán UserId trực tiếp từ User đăng nhập
            model.UserId = userId;

            // Xóa lỗi validation của UserId ra khỏi ModelState vì Client không gửi trường này
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                // Nếu là địa chỉ đầu tiên hoặc người dùng tick chọn mặc định
                var hasAddress = db.UserAddresses.Any(a => a.UserId == userId);
                if (!hasAddress || model.IsDefault)
                {
                    // Bỏ mặc định các địa chỉ cũ
                    var oldDefaults = db.UserAddresses.Where(a => a.UserId == userId && a.IsDefault);
                    foreach (var item in oldDefaults)
                    {
                        item.IsDefault = false;
                    }
                    model.IsDefault = true;
                }

                db.UserAddresses.Add(model);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // GET: UserAddress/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var userId = User.Identity.GetUserId();
            var address = await db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

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
                var addressInDb = await db.UserAddresses.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);
                if (addressInDb == null) return HttpNotFound();

                addressInDb.AddressName = model.AddressName;
                addressInDb.ReceiverName = model.ReceiverName;
                addressInDb.ReceiverPhone = model.ReceiverPhone;
                addressInDb.DetailedAddress = model.DetailedAddress;

                if (model.IsDefault && !addressInDb.IsDefault)
                {
                    var oldDefaults = db.UserAddresses.Where(a => a.UserId == userId && a.IsDefault);
                    foreach (var item in oldDefaults)
                    {
                        item.IsDefault = false;
                    }
                    addressInDb.IsDefault = true;
                }

                await db.SaveChangesAsync();
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
            var address = await db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address != null)
            {
                var allAddresses = db.UserAddresses.Where(a => a.UserId == userId);
                foreach (var item in allAddresses)
                {
                    item.IsDefault = (item.Id == id);
                }
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // POST: UserAddress/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = User.Identity.GetUserId();
            var address = await db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address != null)
            {
                bool wasDefault = address.IsDefault;
                db.UserAddresses.Remove(address);
                await db.SaveChangesAsync();

                if (wasDefault)
                {
                    var firstAddress = db.UserAddresses.FirstOrDefault(a => a.UserId == userId);
                    if (firstAddress != null)
                    {
                        firstAddress.IsDefault = true;
                        await db.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}