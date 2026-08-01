using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Do_An_E_Commerce_BHX.Areas.Admin.Controllers;
using Do_An_E_Commerce_BHX.Models;
using Do_An_E_Commerce_BHX.Models.Entities;
using Do_An_E_Commerce_BHX.Services.Interfaces;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Do_An_E_Commerce_BHX.Services.Implementations
{
    /// <summary>
    /// Service xử lý nghiệp vụ Quản lý người dùng, Phân quyền Identity, Tích điểm thành viên và Lịch sử đơn hàng
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly ApplicationUserManager _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(ApplicationDbContext db, ApplicationUserManager userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Tự động kiểm tra và gán vai trò mặc định "User" cho tài khoản người dùng vừa đăng ký
        /// </summary>
        /// <param name="userId">Mã định danh ID của tài khoản</param>
        public async Task EnsureDefaultUserRoleAssignedAsync(string userId)
        {
            try
            {
                // Khởi tạo Role User nếu cơ sở dữ liệu chưa có
                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                }

                // Gán quyền User cho tài khoản nếu chưa được gán
                var roles = await _userManager.GetRolesAsync(userId);
                if (!roles.Contains("User"))
                {
                    await _userManager.AddToRoleAsync(userId, "User");
                }
            }
            catch { }
        }

        /// <summary>
        /// Tự động cập nhật mốc thời gian hoạt động mới nhất của người dùng khi truy cập hệ thống
        /// </summary>
        public async Task UpdateLastActivityAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    // Chỉ cập nhật nếu lần hoạt động cuối > 1 phút trước để tránh quá tải DB
                    if (!user.LastActivityDate.HasValue || (DateTime.Now - user.LastActivityDate.Value).TotalMinutes >= 1)
                    {
                        user.LastActivityDate = DateTime.Now;
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Lấy danh sách tài khoản người dùng kèm theo vai trò, trạng thái Online/Offline và điểm tích lũy
        /// </summary>
        public async Task<List<UserViewModel>> GetUserViewModelsAsync(string tuKhoa, string roleFilter)
        {
            var usersQuery = _db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string kw = tuKhoa.Trim();
                usersQuery = usersQuery.Where(u => u.UserName.Contains(kw) ||
                                                   u.Email.Contains(kw) ||
                                                   u.FullName.Contains(kw));
            }

            var usersList = await usersQuery.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user.Id);

                if (!string.IsNullOrEmpty(roleFilter) && !roles.Contains(roleFilter))
                {
                    continue;
                }

                // Fallback: Tìm mốc thời gian gần nhất trong UserBehaviorLog hoặc Order nếu LastActivityDate đang NULL
                DateTime? lastActive = user.LastActivityDate;
                if (!lastActive.HasValue)
                {
                    var latestLog = await _db.UserBehaviorLog.AsNoTracking()
                        .Where(l => l.UserId == user.Id)
                        .OrderByDescending(l => l.CreatedDate)
                        .Select(l => (DateTime?)l.CreatedDate)
                        .FirstOrDefaultAsync();

                    var latestOrder = await _db.Order.AsNoTracking()
                        .Where(o => o.UserId == user.Id)
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => (DateTime?)o.OrderDate)
                        .FirstOrDefaultAsync();

                    if (latestLog.HasValue && latestOrder.HasValue)
                    {
                        lastActive = latestLog.Value > latestOrder.Value ? latestLog.Value : latestOrder.Value;
                    }
                    else
                    {
                        lastActive = latestLog ?? latestOrder;
                    }
                }

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    LoyaltyPoints = user.LoyaltyPoints,
                    Roles = roles.ToList(),
                    IsLocked = user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow,
                    LastActivityDate = lastActive
                });
            }

            return userViewModels;
        }

        /// <summary>
        /// Tạo mới tài khoản người dùng kèm phân quyền trực tiếp từ trang Admin
        /// </summary>
        public async Task<IdentityResult> CreateUserAsync(CreateUserViewModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded && !string.IsNullOrEmpty(model.RoleName))
            {
                if (!await _roleManager.RoleExistsAsync(model.RoleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.RoleName));
                }
                await _userManager.AddToRoleAsync(user.Id, model.RoleName);
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin cá nhân và vai trò (Role) của người dùng
        /// </summary>
        public async Task<IdentityResult> EditUserAsync(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return IdentityResult.Failed("Không tìm thấy người dùng!");

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Cập nhật lại danh sách vai trò
                var currentRoles = await _userManager.GetRolesAsync(user.Id);
                await _userManager.RemoveFromRolesAsync(user.Id, currentRoles.ToArray());

                if (!string.IsNullOrEmpty(model.SelectedRole))
                {
                    if (!await _roleManager.RoleExistsAsync(model.SelectedRole))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.SelectedRole));
                    }
                    await _userManager.AddToRoleAsync(user.Id, model.SelectedRole);
                }
            }

            return result;
        }

        /// <summary>
        /// Khóa hoặc Mở khóa tài khoản người dùng (Khóa 100 năm nếu khóa)
        /// </summary>
        public async Task<bool> ToggleLockoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            if (user.LockoutEndDateUtc.HasValue && user.LockoutEndDateUtc.Value > DateTime.UtcNow)
            {
                // Đang bị khóa -> Mở khóa ngay
                await _userManager.SetLockoutEndDateAsync(user.Id, DateTimeOffset.MinValue);
            }
            else
            {
                // Đang hoạt động -> Khóa tài khoản 100 năm
                await _userManager.SetLockoutEnabledAsync(user.Id, true);
                await _userManager.SetLockoutEndDateAsync(user.Id, DateTimeOffset.UtcNow.AddYears(100));
            }

            return true;
        }

        /// <summary>
        /// Đặt lại mật khẩu tài khoản người dùng trực tiếp từ trang Admin
        /// </summary>
        public async Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return IdentityResult.Failed("Không tìm thấy người dùng!");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user.Id);
            return await _userManager.ResetPasswordAsync(user.Id, token, newPassword);
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử đơn hàng của một khách hàng cụ thể (Có lọc theo trạng thái đơn)
        /// </summary>
        public async Task<List<Order>> GetCustomerOrderHistoryAsync(string userId, string statusFilter)
        {
            var query = _db.Order
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .AsNoTracking()
                .Where(o => o.UserId == userId);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                switch (statusFilter.ToLower())
                {
                    case "processing":
                        query = query.Where(o => o.OrderStatus == 0 || o.OrderStatus == 1 || o.OrderStatus == 2 || o.OrderStatus == 3);
                        break;
                    case "completed":
                        query = query.Where(o => o.OrderStatus == 4);
                        break;
                    case "cancelled":
                        query = query.Where(o => o.OrderStatus == 5);
                        break;
                }
            }

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }
    }
}
