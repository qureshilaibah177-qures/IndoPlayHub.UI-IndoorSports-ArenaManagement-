# IndoPlayHub.UI
IndoPlayHub is a .NET MAUI-based indoor sports management and booking application designed to connect customers with indoor sports arenas. The system provides both a customer-facing booking experience and an arena management portal for managing sports, courts, pricing, availability, menu items, and bookings.

# Project Overview
IndoPlayHub is an academic/training project developed to demonstrate how an indoor sports facility booking and management system can be designed using modern software development technologies.

# The application consists of two main sides:
1) Client-Facing Side – Allows customers to explore indoor sports facilities, view available sports and courts, check pricing and availability, and make bookings.
2) Administration/Arena Management Side – Allows arena owners or managers to register their arena and manage sports, courts, pricing, operating hours, menu items, availability, and customer bookings.
3) The project uses a shared database so that arena information and booking data can be managed consistently between the two sides of the application.

Note: IndoPlayHub is currently developed as a training/academic project. Payment processing, OTP verification, email/SMS services, and other production-level integrations are represented through demo functionality where applicable.

# Objectives
The main objectives of IndoPlayHub are to:
Provide a simple platform for indoor sports facility booking.
Allow customers to discover available sports and facilities.
Allow arena owners to manage their facilities.
Manage courts and sports under individual arenas.
Manage pricing and operating hours.
Handle customer bookings.
Maintain booking and payment status.
Demonstrate database-driven application development.
Demonstrate Entity Framework Core integration with SQL Server.
Provide a realistic client and arena-management experience within a training environment.

# Client-Facing Side
The client-facing side is designed for customers who want to find and book indoor sports facilities.
Customers can browse available arenas and sports, view facility information, check pricing and availability, select a date and time, provide their information, and create a booking.
The client side is intended to provide a simple and convenient booking experience similar to the customer side of real-world sports facility platforms.

# Client Features
Browse indoor sports
Browse available arenas
View arena details
View available courts/facilities
View pricing
Check availability
Select booking date
Select booking time
Create bookings
View booking information
View booking status
View payment status
Access booking history
View additional arena services/menu items

# Administration / Arena Management Side
The administration-facing side represents the management portal for individual indoor sports arenas.
Arena owners or managers can register their arena, configure its basic information, and manage the sports and facilities offered by the arena. They can also manage pricing, operating hours, menu items, availability, and customer bookings.

# Technologies Used
C#
.NET MAUI
XAML
Entity Framework Core
SQL Server
Visual Studio
Git / GitHub

# Booking System
The booking system allows a customer to select a sports facility and specify:
Booking date
Start time
End time
Customer information
The system calculates and stores the total booking amount and maintains booking and payment status.
For example, a customer can select a basketball facility with an hourly rate and create a booking for the required time period.

# Arena Management
Each arena can manage its own operational information through the management portal.
Arena management includes:
Arena details
Sports
Courts/facilities
Pricing
Availability
Operating hours
Menu items
Customer bookings
This allows the project to demonstrate a complete facility-management workflow rather than only a simple booking form.
