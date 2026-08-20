using backend.Constants;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PermissionService
{
    private readonly AppDbContext _context;

    public PermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(
        User user,
        string permission)
    {
        if (user.Role == Roles.Admin)
            return true;

        return await _context.UserPermissions
            .AnyAsync(p =>
                p.UserId == user.Id &&
                p.Permission == permission);
    }


    public async Task<bool> GrantPermissionAsync(
        int userId,
        string permission,
        int adminId)
    {
        if (!Permissions.All.Contains(permission))
            return false;

        var alreadyExists =
            await _context.UserPermissions.AnyAsync(p =>
                p.UserId == userId &&
                p.Permission == permission);

        if (alreadyExists)
            return true;

        var userPermission = new UserPermission
        {
            UserId = userId,
            Permission = permission,
            GrantedByAdminId = adminId,
            GrantedAt = DateTime.UtcNow
        };

        _context.UserPermissions.Add(userPermission);

        await _context.SaveChangesAsync();

        return true;
    }


    public async Task<bool> RemovePermissionAsync(
        int userId,
        string permission)
    {
        var userPermission =
            await _context.UserPermissions.FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.Permission == permission);

        if (userPermission == null)
            return false;

        _context.UserPermissions.Remove(userPermission);

        await _context.SaveChangesAsync();

        return true;
    }


    public async Task<List<string>> GetPermissionsAsync(
        int userId)
    {
        return await _context.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();
    }
}