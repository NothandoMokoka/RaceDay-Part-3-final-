using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public ParticipantController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("RoleName") != "Participant")
                return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _apiService.GetAsync("api/Events");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load events.";
                    return View("~/Views/Participant/Index.cshtml",
                        new List<ParticipantEventViewModel>());
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var events = JsonSerializer.Deserialize<List<ParticipantEventViewModel>>(
                    await response.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantEventViewModel>();

                var upcomingEvents = events
                    .Where(e => e.EventDate.Date >= DateTime.Today)
                    .OrderBy(e => e.EventDate)
                    .ToList();

                return View("~/Views/Participant/Index.cshtml", upcomingEvents);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";

                return View("~/Views/Participant/Index.cshtml",
                    new List<ParticipantEventViewModel>());
            }
        }
        public async Task<IActionResult> EventDetails(int id)
        {
            if (HttpContext.Session.GetString("RoleName") != "Participant")
                return RedirectToAction("Login", "Auth");

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var eventResponse = await _apiService.GetAsync($"api/Events/{id}");

                if (!eventResponse.IsSuccessStatusCode)
                    return NotFound();

                var eventItem = JsonSerializer.Deserialize<ParticipantEventViewModel>(
                    await eventResponse.Content.ReadAsStringAsync(), options);

                if (eventItem == null)
                    return NotFound();

                var categoryResponse = await _apiService.GetAsync("api/Categories");

                if (!categoryResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load event categories.";
                    ViewBag.Categories = new List<ParticipantCategoryViewModel>();
                    return View(eventItem);
                }

                var categories = JsonSerializer.Deserialize<List<ParticipantCategoryViewModel>>(
                    await categoryResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantCategoryViewModel>();

                ViewBag.Categories = categories.Where(c => c.EventID == id).ToList();

                return View(eventItem);
            }
            catch (HttpRequestException)
            {
                return RedirectToAction("Index");
            }
        }
// Allows participants to enrol in an event.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int eventId, int categoryId)
        {
            if (HttpContext.Session.GetString("RoleName") != "Participant")
                return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _apiService.PostAsync(
                    "api/Enrollments",
                    new { EventID = eventId, CategoryID = categoryId });

                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "You have successfully enrolled in this event.";
                else
                    TempData["Error"] = "Unable to enrol. Please check your selection and try again.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
            }

            return RedirectToAction("EventDetails", new { id = eventId });
        }

// Retrieves upcoming events for participants.
        public async Task<IActionResult> MyEvents()
        {
            if (HttpContext.Session.GetString("RoleName") != "Participant")
                return RedirectToAction("Login", "Auth");

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var enrollmentResponse = await _apiService.GetAsync("api/Enrollments");

                if (!enrollmentResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load your enrolments.";
                    return View(new List<ParticipantEnrollmentViewModel>());
                }

                var enrollments = JsonSerializer.Deserialize<List<ParticipantEnrollmentViewModel>>(
                    await enrollmentResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantEnrollmentViewModel>();

                var eventResponse = await _apiService.GetAsync("api/Events");
                var categoryResponse = await _apiService.GetAsync("api/Categories");

                if (!eventResponse.IsSuccessStatusCode || !categoryResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load event details.";
                    return View(enrollments);
                }

                var events = JsonSerializer.Deserialize<List<ParticipantEventViewModel>>(
                    await eventResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantEventViewModel>();

                var categories = JsonSerializer.Deserialize<List<ParticipantCategoryViewModel>>(
                    await categoryResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantCategoryViewModel>();

                foreach (var enrollment in enrollments)
                {
                    var eventItem = events.FirstOrDefault(e => e.EventID == enrollment.EventID);
                    var category = categories.FirstOrDefault(c => c.CategoryID == enrollment.CategoryID);

                    if (eventItem != null)
                    {
                        enrollment.EventName = eventItem.Name;
                        enrollment.EventDate = eventItem.EventDate;
                        enrollment.Location = eventItem.Location;
                    }

                    if (category != null)
                        enrollment.CategoryName = category.CategoryName;
                }

                return View(enrollments.OrderByDescending(e => e.EnrollmentDate).ToList());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                return View(new List<ParticipantEnrollmentViewModel>());
            }
        }

        public async Task<IActionResult> MyResults()
        {
            if (HttpContext.Session.GetString("RoleName") != "Participant")
                return RedirectToAction("Login", "Auth");

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var resultResponse = await _apiService.GetAsync("api/Results");

                if (!resultResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load your results.";
                    return View(new List<ParticipantResultViewModel>());
                }

                var results = JsonSerializer.Deserialize<List<ParticipantResultViewModel>>(
                    await resultResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantResultViewModel>();

                var enrollmentResponse = await _apiService.GetAsync("api/Enrollments");
                var eventResponse = await _apiService.GetAsync("api/Events");
                var categoryResponse = await _apiService.GetAsync("api/Categories");

                if (!enrollmentResponse.IsSuccessStatusCode ||
                    !eventResponse.IsSuccessStatusCode ||
                    !categoryResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load result details.";
                    return View(results);
                }

                var enrollments = JsonSerializer.Deserialize<List<ParticipantEnrollmentViewModel>>(
                    await enrollmentResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantEnrollmentViewModel>();

                var events = JsonSerializer.Deserialize<List<ParticipantEventViewModel>>(
                    await eventResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantEventViewModel>();

                var categories = JsonSerializer.Deserialize<List<ParticipantCategoryViewModel>>(
                    await categoryResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<ParticipantCategoryViewModel>();

                foreach (var result in results)
                {
                    var enrollment = enrollments.FirstOrDefault(
                        e => e.EnrollmentID == result.EnrollmentID);

                    if (enrollment != null)
                    {
                        var eventItem = events.FirstOrDefault(e => e.EventID == enrollment.EventID);
                        var category = categories.FirstOrDefault(c => c.CategoryID == enrollment.CategoryID);

                        if (eventItem != null)
                        {
                            result.EventName = eventItem.Name;
                            result.EventDate = eventItem.EventDate;
                        }

                        if (category != null)
                            result.CategoryName = category.CategoryName;
                    }
                }

                return View(results.OrderBy(r => r.FinishPosition).ToList());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                return View(new List<ParticipantResultViewModel>());
            }
        }

        public async Task<IActionResult> Profile()
        {
            if (HttpContext.Session.GetString("RoleName") != "Participant")
                return RedirectToAction("Login", "Auth");

            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            if (userId == 0)
                return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _apiService.GetAsync($"api/Users/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to load your profile.";
                    return View(new ParticipantProfileViewModel());
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var profile = JsonSerializer.Deserialize<ParticipantProfileViewModel>(
                    await response.Content.ReadAsStringAsync(), options);

                return View(profile ?? new ParticipantProfileViewModel());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                return View(new ParticipantProfileViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ParticipantProfileViewModel model)
        {
            if (HttpContext.Session.GetString("RoleName") != "Participant")
                return RedirectToAction("Login", "Auth");

            int userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            if (userId == 0)
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _apiService.PutAsync(
                    $"api/Users/{userId}",
                    new
                    {
                        model.FirstName,
                        model.LastName,
                        model.PhoneNumber
                    });

                if (response.IsSuccessStatusCode)
                {
                    HttpContext.Session.SetString("FirstName", model.FirstName);
                    HttpContext.Session.SetString("LastName", model.LastName);
                    TempData["Success"] = "Your profile has been updated.";
                    return RedirectToAction("Profile");
                }

                ViewBag.Error = "Unable to update your profile.";
                return View(model);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profileImage)
        {
            if (HttpContext.Session.GetString("RoleName") != "Participant")
                return RedirectToAction("Login", "Auth");

            if (profileImage == null || profileImage.Length == 0)
            {
                TempData["Error"] = "Please select an image.";
                return RedirectToAction("Profile");
            }

            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = profileImage.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue(profileImage.ContentType);

                content.Add(streamContent, "file", profileImage.FileName);

                var response = await _apiService.PostMultipartAsync(
                    "api/ImageUpload/profile-picture", content);

                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "Your profile picture has been uploaded.";
                else
                    TempData["Error"] = "Unable to upload your picture. Check that it is JPG, PNG or WEBP and under 5 MB.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
            }

            return RedirectToAction("Profile");
        }
    }
}
