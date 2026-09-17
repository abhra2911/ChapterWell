# ChapterWell

A library management system built with ASP.NET Core MVC and Entity Framework Core.

## Overview

ChapterWell is a dual-portal web application for running a library: librarians manage the catalog, members, borrowings, and fines, while members browse the catalog, track loans, manage a wishlist, and pay fines. It was built during an Application Developer internship and migrated from an original Oracle 19c / .NET Core 2.1 stack to SQL Server / .NET 10.

## Tech stack

- ASP.NET Core MVC on .NET 10
- Entity Framework Core 10 with the SQL Server provider
- SQL Server (originally Oracle 19c via Devart dotConnect)
- Bootstrap 5 and Bootstrap Icons
- Chart.js for dashboard visualizations
- BCrypt.Net-Next for password hashing

## Features

**Librarian portal**
- Dashboard with library-wide stats and charts
- Book catalog management: add, edit, delete, shelf assignment
- Member management: add, edit, delete
- Issue and return books, fulfill reservations
- Bulk operations (multi-select delete/return)
- Damage reporting for returned books
- Fine tracking, including fine crystallization on renewal/return
- CSV report export with date-range filters
- Profile editing and password change

**Member portal**
- Dashboard with personal loan and fine summary
- Catalog browsing with author autocomplete
- Loan history and active loans, with renewal
- Reservations, including an atomic reserve-then-fulfill workflow
- Wishlist management
- Fine payment, individually or in full
- Profile editing and password change

**Shared**
- Session-backed authentication for librarian and member accounts
- Responsive layouts using CSS grid split panels
- Animation-free theme switching on Chart.js dashboards

## Project structure

```
Controllers/     AccountController (auth + both portals), HomeController
Models/          Entity classes, view models, and DTOs
Data/            ModelContext (EF Core), LibraryRepository, CurrentUser
Reporting/       IReportExporter and CsvReportExporter for CSV report export
Views/           Razor views, split into Home and Account
wwwroot/         Static assets: CSS, JS, images, client libraries
```

## Database

The application targets SQL Server. Table names use a `Libmgmt`-prefixed naming convention carried over from the Oracle migration. Primary keys are application-assigned (no IDENTITY columns), matching the original Oracle scaffolding.

Set the connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "LibraryDb": "Server=localhost;Database=ChapterWell;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

A schema matching the `ModelContext` entities must exist on the target server before running the application; this repository does not include a schema or seed script.
