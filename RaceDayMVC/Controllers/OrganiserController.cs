using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;
using RaceDayMVC.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RaceDayMVC.Controllers
{
    public class OrganizerController : Controller
    {
        private readonly RaceDayApiService _apiService;

        public OrganizerController(RaceDayApiService apiService)
        {
            _apiService = apiService;
        }

       // Displays the organizer dashboard. 
        public async Task<IActionResult> Index()
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var eventsResponse = await _apiService.GetAsync("api/Events");
                var enrollmentsResponse = await _apiService.GetAsync("api/Enrollments");

                if (eventsResponse.IsSuccessStatusCode &&
                    enrollmentsResponse.IsSuccessStatusCode)
                {
                    var events = JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                        await eventsResponse.Content.ReadAsStringAsync(), options)
                        ?? new List<OrganizerEventViewModel>();

                    var enrollments = JsonSerializer.Deserialize<List<OrganizerEnrollmentViewModel>>(
                        await enrollmentsResponse.Content.ReadAsStringAsync(), options)
                        ?? new List<OrganizerEnrollmentViewModel>();

                    int organizerID = HttpContext.Session.GetInt32("UserID") ?? 0;

                    var myEvents = events
                        .Where(e => e.OrganizerID == organizerID)
                        .ToList();

                    var eventIDs = myEvents.Select(e => e.EventID).ToList();

                    var myEnrollments = enrollments
                        .Where(e => eventIDs.Contains(e.EventID))
                        .ToList();

                    ViewBag.TotalEvents = myEvents.Count;
                    ViewBag.TotalEnrollments = myEnrollments.Count;
                    ViewBag.UpcomingEvents = myEvents.Count(
                        e => e.EventDate.Date >= DateTime.Today);
                }
                else
                {
                    ViewBag.TotalEvents = 0;
                    ViewBag.TotalEnrollments = 0;
                    ViewBag.UpcomingEvents = 0;
                }
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                ViewBag.TotalEvents = 0;
                ViewBag.TotalEnrollments = 0;
                ViewBag.UpcomingEvents = 0;
            }

            return View();
        }
// Creates a new sporting event.
        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            return View(new EventViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventViewModel model)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _apiService.PostAsync(
                    "api/Events",
                    new
                    {
                        model.Name,
                        model.Description,
                        model.EventDate,
                        model.Location,
                        model.Distance,
                        model.EventType
                    });

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to create event. Please check your details and try again.";
                    return View(model);
                }

                string responseBody =
                    await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(responseBody);

                int eventId = 0;

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (string.Equals(property.Name, "EventID",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        eventId = property.Value.GetInt32();
                        break;
                    }
                }

                if (eventId == 0)
                {
                    TempData["Error"] =
                        "The event was created, but its ID could not be read.";
                    return RedirectToAction("Index");
                }

                if (model.BannerImage != null &&
                    model.BannerImage.Length > 0)
                {
                    using var content = new MultipartFormDataContent();

                    using var stream = model.BannerImage.OpenReadStream();

                    using var fileContent = new StreamContent(stream);

                    fileContent.Headers.ContentType =
                        new MediaTypeHeaderValue(model.BannerImage.ContentType);

                    content.Add(fileContent, "file",
                        model.BannerImage.FileName);

                    var uploadResponse =
                        await _apiService.PostMultipartAsync(
                            $"api/ImageUpload/event-banner/{eventId}",
                            content);

                    if (!uploadResponse.IsSuccessStatusCode)
                    {
                        TempData["Error"] =
                            "Event created successfully, but the banner upload failed.";
                        return RedirectToAction("Index");
                    }
                }

                TempData["Success"] = "Event created successfully!";

                return RedirectToAction("Index");
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Could not connect to the RaceDay API. Make sure the API is running.";

                return View(model);
            }
            catch (Exception)
            {
                ViewBag.Error =
                    "Something went wrong while creating the event.";

                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _apiService.GetAsync($"api/Events/{id}");

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var responseBody = await response.Content.ReadAsStringAsync();

                var eventItem =
                    JsonSerializer.Deserialize<OrganizerEventViewModel>(
                        responseBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                int organizerID =
                    HttpContext.Session.GetInt32("UserID")!.Value;

                if (eventItem == null || eventItem.OrganizerID != organizerID)
                    return NotFound();

                var model = new EventViewModel
                {
                    EventID = eventItem.EventID,
                    Name = eventItem.Name,
                    Description = eventItem.Description,
                    EventDate = eventItem.EventDate,
                    Location = eventItem.Location,
                    Distance = eventItem.Distance,
                    EventType = eventItem.EventType,
                    BannerImageUrl = eventItem.BannerImageUrl
                };

                return View(model);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
                return RedirectToAction("Events");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EventViewModel model)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _apiService.PutAsync(
                    $"api/Events/{model.EventID}",
                    new
                    {
                        model.Name,
                        model.Description,
                        model.EventDate,
                        model.Location,
                        model.Distance,
                        model.EventType
                    });

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to update event. Please try again.";
                    return View(model);
                }

                if (model.BannerImage != null &&
                    model.BannerImage.Length > 0)
                {
                    using var content = new MultipartFormDataContent();

                    using var stream = model.BannerImage.OpenReadStream();

                    using var fileContent = new StreamContent(stream);

                    fileContent.Headers.ContentType =
                        new MediaTypeHeaderValue(model.BannerImage.ContentType);

                    content.Add(fileContent, "file", model.BannerImage.FileName);

                    var uploadResponse =
                        await _apiService.PostMultipartAsync(
                            $"api/ImageUpload/event-banner/{model.EventID}",
                            content);

                    if (!uploadResponse.IsSuccessStatusCode)
                    {
                        TempData["Error"] =
                            "Event updated, but the banner upload failed.";
                        return RedirectToAction("Events");
                    }
                }

                TempData["Success"] = "Event updated successfully!";

                return RedirectToAction("Events");
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                return View(model);
            }
        }
        // Display events belonging to the logged-in organizer
        public async Task<IActionResult> Events()
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _apiService.GetAsync("api/Events");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to retrieve your events.";
                    return View(new List<OrganizerEventViewModel>());
                }

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                var allEvents =
                    JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                        responseBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrganizerEventViewModel>();

                int organizerID =
                    HttpContext.Session.GetInt32("UserID")!.Value;

                var myEvents = allEvents
                    .Where(e => e.OrganizerID == organizerID)
                    .OrderByDescending(e => e.EventDate)
                    .ToList();

                return View(myEvents);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Could not connect to the RaceDay API. Make sure it is running.";

                return View(new List<OrganizerEventViewModel>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _apiService.GetAsync($"api/Events/{id}");

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var responseBody = await response.Content.ReadAsStringAsync();

                var eventItem =
                    JsonSerializer.Deserialize<OrganizerEventViewModel>(
                        responseBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                int organizerID =
                    HttpContext.Session.GetInt32("UserID")!.Value;

                if (eventItem == null || eventItem.OrganizerID != organizerID)
                    return NotFound();

                return View(eventItem);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
                return RedirectToAction("Events");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventConfirmed(int id)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _apiService.DeleteAsync($"api/Events/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] =
                        "Unable to delete event. Please try again.";

                    return RedirectToAction("Events");
                }

                TempData["Success"] = "Event deleted successfully!";

                return RedirectToAction("Events");
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
                return RedirectToAction("Events");
            }
        }
        public async Task<IActionResult> Categories()
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                // Get all events from the API
                var eventsResponse = await _apiService.GetAsync("api/Events");

                if (!eventsResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to retrieve your events.";
                    return View(new List<OrganizerCategoryViewModel>());
                }

                var eventsBody = await eventsResponse.Content.ReadAsStringAsync();

                var allEvents =
                    JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                        eventsBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrganizerEventViewModel>();

                int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

                var myEvents = allEvents
                    .Where(e => e.OrganizerID == organizerID)
                    .ToList();

                var myEventIDs = myEvents.Select(e => e.EventID).ToList();

                // Get all categories from the API
                var categoriesResponse =
                    await _apiService.GetAsync("api/Categories");

                if (!categoriesResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to retrieve categories.";
                    return View(new List<OrganizerCategoryViewModel>());
                }

                var categoriesBody =
                    await categoriesResponse.Content.ReadAsStringAsync();

                var allCategories =
                    JsonSerializer.Deserialize<List<OrganizerCategoryViewModel>>(
                        categoriesBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrganizerCategoryViewModel>();

                // Keep only categories belonging to this organizer's events
                var myCategories = allCategories
                    .Where(c => myEventIDs.Contains(c.EventID))
                    .ToList();

                // Add the corresponding event name
                foreach (var category in myCategories)
                {
                    category.EventName = myEvents
                        .First(e => e.EventID == category.EventID).Name;
                }

                ViewBag.Events = myEvents;

                return View(myCategories);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                return View(new List<OrganizerCategoryViewModel>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> CreateCategory()
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var response = await _apiService.GetAsync("api/Events");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Unable to load your events.";
                    return RedirectToAction("Categories");
                }

                var body = await response.Content.ReadAsStringAsync();

                var allEvents =
                    JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                        body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrganizerEventViewModel>();

                int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

                ViewBag.Events = allEvents
                    .Where(e => e.OrganizerID == organizerID)
                    .ToList();

                return View(new CategoryViewModel());
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
                return RedirectToAction("Categories");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                await LoadOrganizerEvents();
                return View(model);
            }

            if (model.MinimumAge.HasValue &&
                model.MaximumAge.HasValue &&
                model.MinimumAge > model.MaximumAge)
            {
                ModelState.AddModelError(
                    nameof(model.MinimumAge),
                    "Minimum age cannot exceed maximum age.");

                await LoadOrganizerEvents();
                return View(model);
            }

            try
            {
                var response = await _apiService.PostAsync(
                    "api/Categories",
                    new
                    {
                        model.EventID,
                        model.CategoryName,
                        model.MinimumAge,
                        model.MaximumAge,
                        model.Distance
                    });

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        "Unable to create category. Check your details and try again.";

                    await LoadOrganizerEvents();
                    return View(model);
                }

                TempData["Success"] = "Category created successfully!";

                return RedirectToAction("Categories");
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";

                await LoadOrganizerEvents();
                return View(model);
            }
        }

        private async Task LoadOrganizerEvents()
        {
            var response = await _apiService.GetAsync("api/Events");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Events = new List<OrganizerEventViewModel>();
                return;
            }

            var body = await response.Content.ReadAsStringAsync();

            var allEvents =
                JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<OrganizerEventViewModel>();

            int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

            ViewBag.Events = allEvents
                .Where(e => e.OrganizerID == organizerID)
                .ToList();
        }
        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var categoryResponse =
                    await _apiService.GetAsync($"api/Categories/{id}");

                if (!categoryResponse.IsSuccessStatusCode)
                    return NotFound();

                var categoryBody =
                    await categoryResponse.Content.ReadAsStringAsync();

                var model =
                    JsonSerializer.Deserialize<CategoryViewModel>(
                        categoryBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (model == null)
                    return NotFound();

                var eventsResponse = await _apiService.GetAsync("api/Events");

                if (!eventsResponse.IsSuccessStatusCode)
                    return RedirectToAction("Categories");

                var eventsBody = await eventsResponse.Content.ReadAsStringAsync();

                var allEvents =
                    JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                        eventsBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrganizerEventViewModel>();

                int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

                var myEvents = allEvents
                    .Where(e => e.OrganizerID == organizerID)
                    .ToList();

                if (!myEvents.Any(e => e.EventID == model.EventID))
                    return NotFound();

                ViewBag.Events = myEvents;

                return View(model);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
                return RedirectToAction("Categories");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryViewModel model)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var categoryResponse =
                    await _apiService.GetAsync($"api/Categories/{model.CategoryID}");

                if (!categoryResponse.IsSuccessStatusCode)
                    return NotFound();

                var categoryBody =
                    await categoryResponse.Content.ReadAsStringAsync();

                var existingCategory =
                    JsonSerializer.Deserialize<CategoryViewModel>(
                        categoryBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (existingCategory == null)
                    return NotFound();

                var eventsResponse = await _apiService.GetAsync("api/Events");

                if (!eventsResponse.IsSuccessStatusCode)
                    return RedirectToAction("Categories");

                var eventsBody = await eventsResponse.Content.ReadAsStringAsync();

                var allEvents =
                    JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                        eventsBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrganizerEventViewModel>();

                int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

                var myEvents = allEvents
                    .Where(e => e.OrganizerID == organizerID)
                    .ToList();

                // Confirm the existing category belongs to this organizer
                if (!myEvents.Any(e => e.EventID == existingCategory.EventID))
                    return NotFound();

                // Confirm the selected event also belongs to this organizer
                if (!myEvents.Any(e => e.EventID == model.EventID))
                {
                    ModelState.AddModelError(
                        nameof(model.EventID),
                        "Please select one of your own events.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Events = myEvents;
                    return View(model);
                }

                if (model.MinimumAge.HasValue &&
                    model.MaximumAge.HasValue &&
                    model.MinimumAge > model.MaximumAge)
                {
                    ModelState.AddModelError(
                        nameof(model.MinimumAge),
                        "Minimum age cannot exceed maximum age.");

                    ViewBag.Events = myEvents;
                    return View(model);
                }

                var response = await _apiService.PutAsync(
                    $"api/Categories/{model.CategoryID}",
                    new
                    {
                        model.EventID,
                        model.CategoryName,
                        model.MinimumAge,
                        model.MaximumAge,
                        model.Distance
                    });

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        "Unable to update category. Check your details and try again.";

                    ViewBag.Events = myEvents;
                    return View(model);
                }

                TempData["Success"] = "Category updated successfully!";

                return RedirectToAction("Categories");
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
                return RedirectToAction("Categories");
            }
        }
        [HttpGet]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var response =
                    await _apiService.GetAsync($"api/Categories/{id}");

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var body = await response.Content.ReadAsStringAsync();

                var category =
                    JsonSerializer.Deserialize<CategoryViewModel>(
                        body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (category == null)
                    return NotFound();

                var eventsResponse = await _apiService.GetAsync("api/Events");

                if (!eventsResponse.IsSuccessStatusCode)
                    return RedirectToAction("Categories");

                var eventsBody = await eventsResponse.Content.ReadAsStringAsync();

                var allEvents =
                    JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                        eventsBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrganizerEventViewModel>();

                int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

                var belongsToOrganizer = allEvents.Any(e =>
                    e.EventID == category.EventID &&
                    e.OrganizerID == organizerID);

                if (!belongsToOrganizer)
                    return NotFound();

                return View(category);
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
                return RedirectToAction("Categories");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(int id)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                // Verify that the category belongs to this organizer
                var categoryResponse =
                    await _apiService.GetAsync($"api/Categories/{id}");

                if (!categoryResponse.IsSuccessStatusCode)
                    return NotFound();

                var categoryBody =
                    await categoryResponse.Content.ReadAsStringAsync();

                var category =
                    JsonSerializer.Deserialize<CategoryViewModel>(
                        categoryBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (category == null)
                    return NotFound();

                var eventsResponse = await _apiService.GetAsync("api/Events");

                if (!eventsResponse.IsSuccessStatusCode)
                    return RedirectToAction("Categories");

                var eventsBody = await eventsResponse.Content.ReadAsStringAsync();

                var allEvents =
                    JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                        eventsBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrganizerEventViewModel>();

                int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

                if (!allEvents.Any(e =>
                    e.EventID == category.EventID &&
                    e.OrganizerID == organizerID))
                {
                    return NotFound();
                }

                var response =
                    await _apiService.DeleteAsync($"api/Categories/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] =
                        "Unable to delete this category. It may already have enrolments.";

                    return RedirectToAction("Categories");
                }

                TempData["Success"] = "Category deleted successfully!";

                return RedirectToAction("Categories");
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "Could not connect to the RaceDay API.";
                return RedirectToAction("Categories");
            }
        }
        public async Task<IActionResult> Enrolments()
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var eventsResponse = await _apiService.GetAsync("api/Events");
                var categoriesResponse = await _apiService.GetAsync("api/Categories");
                var enrollmentsResponse = await _apiService.GetAsync("api/Enrollments");
                var usersResponse = await _apiService.GetAsync("api/Users");

                if (!eventsResponse.IsSuccessStatusCode ||
                    !categoriesResponse.IsSuccessStatusCode ||
                    !enrollmentsResponse.IsSuccessStatusCode ||
                    !usersResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to retrieve enrolment information.";
                    return View(new List<OrganizerEnrollmentViewModel>());
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var events = JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                    await eventsResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerEventViewModel>();

                var categories = JsonSerializer.Deserialize<List<OrganizerCategoryViewModel>>(
                    await categoriesResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerCategoryViewModel>();

                var enrollments = JsonSerializer.Deserialize<List<OrganizerEnrollmentViewModel>>(
                    await enrollmentsResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerEnrollmentViewModel>();

                var participants = JsonSerializer.Deserialize<List<OrganizerParticipantViewModel>>(
                    await usersResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerParticipantViewModel>();

                int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

                var myEvents = events
                    .Where(e => e.OrganizerID == organizerID)
                    .ToList();

                var myEventIDs = myEvents.Select(e => e.EventID).ToList();

                var myEnrollments = enrollments
                    .Where(e => myEventIDs.Contains(e.EventID))
                    .ToList();

                foreach (var enrollment in myEnrollments)
                {
                    var eventItem = myEvents.FirstOrDefault(
                        e => e.EventID == enrollment.EventID);

                    var category = categories.FirstOrDefault(
                        c => c.CategoryID == enrollment.CategoryID);

                    var participant = participants.FirstOrDefault(
                        p => p.UserID == enrollment.ParticipantID);

                    enrollment.EventName = eventItem?.Name ?? "Unknown Event";
                    enrollment.CategoryName = category?.CategoryName ?? "Unknown Category";

                    enrollment.ParticipantName = participant == null
                        ? "Unknown Participant"
                        : $"{participant.FirstName} {participant.LastName}";

                    enrollment.ParticipantEmail = participant?.Email ?? "Unavailable";
                }

                return View(myEnrollments
                    .OrderByDescending(e => e.EnrollmentDate)
                    .ToList());
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                return View(new List<OrganizerEnrollmentViewModel>());
            }
        }
        public async Task<IActionResult> Results()
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            try
            {
                var resultsResponse = await _apiService.GetAsync("api/Results");
                var eventsResponse = await _apiService.GetAsync("api/Events");
                var enrollmentsResponse = await _apiService.GetAsync("api/Enrollments");
                var categoriesResponse = await _apiService.GetAsync("api/Categories");
                var usersResponse = await _apiService.GetAsync("api/Users");

                if (!resultsResponse.IsSuccessStatusCode ||
                    !eventsResponse.IsSuccessStatusCode ||
                    !enrollmentsResponse.IsSuccessStatusCode ||
                    !categoriesResponse.IsSuccessStatusCode ||
                    !usersResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Unable to retrieve results.";
                    return View(new List<OrganizerResultViewModel>());
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var results = JsonSerializer.Deserialize<List<OrganizerResultViewModel>>(
                    await resultsResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerResultViewModel>();

                var events = JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                    await eventsResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerEventViewModel>();

                var enrollments = JsonSerializer.Deserialize<List<OrganizerEnrollmentViewModel>>(
                    await enrollmentsResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerEnrollmentViewModel>();

                var categories = JsonSerializer.Deserialize<List<OrganizerCategoryViewModel>>(
                    await categoriesResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerCategoryViewModel>();

                var participants = JsonSerializer.Deserialize<List<OrganizerParticipantViewModel>>(
                    await usersResponse.Content.ReadAsStringAsync(), options)
                    ?? new List<OrganizerParticipantViewModel>();

                int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

                var myEventIDs = events
                    .Where(e => e.OrganizerID == organizerID)
                    .Select(e => e.EventID)
                    .ToList();

                var myEnrollments = enrollments
                    .Where(e => myEventIDs.Contains(e.EventID))
                    .ToList();

                var myEnrollmentIDs = myEnrollments
                    .Select(e => e.EnrollmentID)
                    .ToList();

                var myResults = results
                    .Where(r => myEnrollmentIDs.Contains(r.EnrollmentID))
                    .ToList();

                foreach (var result in myResults)
                {
                    var enrollment = myEnrollments.FirstOrDefault(
                        e => e.EnrollmentID == result.EnrollmentID);

                    if (enrollment == null)
                        continue;

                    var eventItem = events.FirstOrDefault(
                        e => e.EventID == enrollment.EventID);

                    var category = categories.FirstOrDefault(
                        c => c.CategoryID == enrollment.CategoryID);

                    var participant = participants.FirstOrDefault(
                        p => p.UserID == enrollment.ParticipantID);

                    result.EventName = eventItem?.Name ?? "Unknown Event";
                    result.CategoryName = category?.CategoryName ?? "Unknown Category";

                    result.ParticipantName = participant == null
                        ? "Unknown Participant"
                        : $"{participant.FirstName} {participant.LastName}";
                }

                return View(myResults);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "Could not connect to the RaceDay API.";
                return View(new List<OrganizerResultViewModel>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> RecordResult()
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            await LoadConfirmedEnrollments();

            return View(new RecordResultViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordResult(RecordResultViewModel model)
        {
            if (!IsOrganizer())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                await LoadConfirmedEnrollments();
                return View(model);
            }

            try
            {
                var response = await _apiService.PostAsync("api/Results", new
                {
                    model.EnrollmentID,
                    model.FinishPosition,
                    model.FinishTime
                });

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Result recorded successfully.";
                    return RedirectToAction("Results");
                }

                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", error);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "Could not connect to the RaceDay API.");
            }

            await LoadConfirmedEnrollments();
            return View(model);
        }

        private async Task LoadConfirmedEnrollments()
        {
            var eventsResponse = await _apiService.GetAsync("api/Events");
            var enrollmentsResponse = await _apiService.GetAsync("api/Enrollments");
            var categoriesResponse = await _apiService.GetAsync("api/Categories");
            var usersResponse = await _apiService.GetAsync("api/Users");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            if (!eventsResponse.IsSuccessStatusCode ||
                !enrollmentsResponse.IsSuccessStatusCode ||
                !categoriesResponse.IsSuccessStatusCode ||
                !usersResponse.IsSuccessStatusCode)
            {
                ViewBag.Enrollments = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                return;
            }

            var events = JsonSerializer.Deserialize<List<OrganizerEventViewModel>>(
                await eventsResponse.Content.ReadAsStringAsync(), options)
                ?? new List<OrganizerEventViewModel>();

            var enrollments = JsonSerializer.Deserialize<List<OrganizerEnrollmentViewModel>>(
                await enrollmentsResponse.Content.ReadAsStringAsync(), options)
                ?? new List<OrganizerEnrollmentViewModel>();

            var categories = JsonSerializer.Deserialize<List<OrganizerCategoryViewModel>>(
                await categoriesResponse.Content.ReadAsStringAsync(), options)
                ?? new List<OrganizerCategoryViewModel>();

            var participants = JsonSerializer.Deserialize<List<OrganizerParticipantViewModel>>(
                await usersResponse.Content.ReadAsStringAsync(), options)
                ?? new List<OrganizerParticipantViewModel>();

            int organizerID = HttpContext.Session.GetInt32("UserID")!.Value;

            var myEventIDs = events
                .Where(e => e.OrganizerID == organizerID)
                .Select(e => e.EventID)
                .ToList();

            var confirmedEnrollments = enrollments
                .Where(e => myEventIDs.Contains(e.EventID) &&
                            e.Status == "CONFIRMED")
                .ToList();

            var items = confirmedEnrollments.Select(e =>
            {
                var eventItem = events.FirstOrDefault(x => x.EventID == e.EventID);
                var category = categories.FirstOrDefault(x => x.CategoryID == e.CategoryID);
                var participant = participants.FirstOrDefault(x => x.UserID == e.ParticipantID);

                return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.EnrollmentID.ToString(),
                    Text = $"{participant?.FirstName} {participant?.LastName} - {eventItem?.Name} - {category?.CategoryName}"
                };
            }).ToList();

            ViewBag.Enrollments = items;
        }
        private bool IsOrganizer()
        {
            return HttpContext.Session.GetInt32("UserID") != null
                && HttpContext.Session.GetString("RoleName") == "Organizer";
        }
    }
}
