# RaceDay-Part-3-final-
Automation of Web Application
RaceDay – Part 3

Youtube Link: 

Project Overview

RaceDay is a sports event management system developed as part of the PROG6212 Programming Project. Part 3 focuses on developing an ASP.NET Core MVC web application that communicates with the RaceDay Web API developed in Part 2.
The system allows organisers to manage sporting events, categories, participant enrolments and race results, while participants can browse events, enrol in competitions and view their results.

Technologies Used
* C#
* ASP.NET Core MVC (.NET 8)
* ASP.NET Core Web API
* Entity Framework Core
* Microsoft SQL Server
* HTML, CSS and Bootstrap
* JavaScript
* Azure Blob Storage
* Docker

Main Features
Authentication and User Management

* User login and registration.
* Role-based access for Organisers and Participants.
* Session-based authentication.
* Profile viewing and updating.
* Profile picture uploads.

Participant Features
* View upcoming sporting events.
* View event details and categories.
* Enrol in sporting events.
* View enrolled events.
* View race results.
* Update personal profile information.
* Upload profile pictures.

Organiser Features
* View the organiser dashboard.
* Create, edit and delete sporting events.
* Manage event categories.
* View and manage participant enrolments.
* Record and manage race results.
* Upload event banner images.

Image Storage
Azure Blob Storage is integrated through the Web API to store event banner images and participant profile pictures.

Docker
A Dockerfile is included for containerising the ASP.NET Core MVC application.

Project Structure
The solution contains two main projects:

* RaceDayAPI: Handles API requests, database operations, authentication and Azure image uploads.
* RaceDayMVC: Provides the web interface and communicates with the Web API.

How to Run the Project

Requirements
* Visual Studio 2022.
* .NET 8 SDK.
* Microsoft SQL Server.
* SQL Server Management Studio (SSMS).
* Docker Desktop (for container execution).

Running the API
1. Open the RaceDayAPI project in Visual Studio.
2. Check the database connection string in the configuration.
3. Ensure SQL Server is running.
4. Run the API project.
5. Open Swagger to view and test the API endpoints.

Running the MVC Application

1. Open the RaceDayMVC project in Visual Studio.
2. Ensure the RaceDayAPI is running.
3. Check that the API BaseUrl in appsettings.json points to the correct API address.
4. Run the RaceDayMVC project.
5. Open the application in your browser.

Database
The application uses Microsoft SQL Server to store information relating to:
* Users and roles.
* Sporting events.
* Event categories.
* Participant enrolments.
* Race results.
