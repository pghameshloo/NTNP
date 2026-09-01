using Microsoft.AspNetCore.Identity;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Identity;
using NTNP.Pricing.Infrastructure.Seed;

namespace NTNP.Pricing.Api.Tools;

/// <summary>
/// Section 35 — the "Initial Admin creation utility". Invoked as
/// <c>NTNP.Pricing.Api.exe create-admin [--email x] [--display-name x] [--password x]</c> (see
/// deployment/database/create-admin.ps1) instead of starting the web host, so a fresh production
/// database gets its first Admin account through the real Identity stack (correct password
/// hashing, role assignment, audit-consistent) rather than a hand-rolled SQL insert. Safe to run
/// again later — it will refuse to create a second account for an email that already exists rather
/// than silently resetting its password.
/// </summary>
public static class AdminBootstrap
{
    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await IdentitySeeder.EnsureRolesAsync(roleManager);

        var email = GetArgValue(args, "--email") ?? Prompt("Admin email address");
        var displayName = GetArgValue(args, "--display-name") ?? Prompt("Admin display name", defaultValue: "System Administrator");
        var password = GetArgValue(args, "--password") ?? PromptPassword("Admin password (min 8 chars, upper+lower+digit+symbol)");

        if (string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine("An email address is required.");
            return 1;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            Console.Error.WriteLine($"A user with email '{email}' already exists. Use the Users and Roles screen (or the reset-password endpoint) to change its password instead — this utility never overwrites an existing account.");
            return 1;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            Console.Error.WriteLine("Failed to create the admin account:");
            foreach (var error in result.Errors)
                Console.Error.WriteLine($"  - {error.Description}");
            return 1;
        }

        await userManager.AddToRoleAsync(admin, Roles.Admin);

        Console.WriteLine();
        Console.WriteLine($"Admin account '{email}' created successfully with the Admin role.");
        Console.WriteLine("Sign in from the desktop client's Login screen using this email and the password you provided.");
        return 0;
    }

    private static string? GetArgValue(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string Prompt(string label, string? defaultValue = null)
    {
        Console.Write(defaultValue is null ? $"{label}: " : $"{label} [{defaultValue}]: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? defaultValue ?? string.Empty : input;
    }

    /// <summary>Reads a password from the console without echoing it to the screen.</summary>
    private static string PromptPassword(string label)
    {
        Console.Write($"{label}: ");
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0) password.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar))
                password.Append(key.KeyChar);
        }
        return password.ToString();
    }
}
