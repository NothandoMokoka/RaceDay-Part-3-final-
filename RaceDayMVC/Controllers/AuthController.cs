using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public AuthController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _apiService.PostAsync(
                "api/Auth/login",
                new
                {
                    model.Email,
                    model.Password
                });

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = GetErrorMessage(responseBody);
                return View(model);
            }

            using var document = JsonDocument.Parse(responseBody);

            var user = GetPropertyIgnoreCase(
                document.RootElement, "user");

            HttpContext.Session.SetInt32(
                "UserID",
                GetPropertyIgnoreCase(user, "userID").GetInt32());

            HttpContext.Session.SetString(
                "RoleName",
                GetPropertyIgnoreCase(user, "roleName").GetString() ?? "");

            HttpContext.Session.SetString(
                "FirstName",
                GetPropertyIgnoreCase(user, "firstName").GetString() ?? "");

            HttpContext.Session.SetString(
                "LastName",
                GetPropertyIgnoreCase(user, "lastName").GetString() ?? "");

            HttpContext.Session.SetString(
                "Email",
                GetPropertyIgnoreCase(user, "email").GetString() ?? "");

            string role = HttpContext.Session.GetString("RoleName") ?? "";

            if (role == "Organizer")
                return RedirectToAction("Index", "Organizer");

            if (role == "Participant")
                return RedirectToAction("Index", "Participant");

            HttpContext.Session.Clear();

            ViewBag.Error = "Your account has an invalid role.";
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var response = await _apiService.PostAsync(
                "api/Auth/register",
                new
                {
                    model.FirstName,
                    model.LastName,
                    model.Email,
                    model.PhoneNumber,
                    model.Password
                });

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = GetErrorMessage(responseBody);
                return View(model);
            }

            TempData["Success"] =
                "Registration successful! You can now log in.";

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _apiService.PostAsync("api/Auth/logout");

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        private static JsonElement GetPropertyIgnoreCase(
            JsonElement element, string propertyName)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            throw new JsonException(
                $"The API response is missing {propertyName}.");
        }

        private static string GetErrorMessage(string responseBody)
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (string.Equals(
                        property.Name,
                        "message",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return property.Value.GetString()
                            ?? "Something went wrong.";
                    }
                }
            }
            catch (JsonException)
            {
                // Use the default message below.
            }

            return "Unable to complete your request. Please try again.";
        }
    }
}