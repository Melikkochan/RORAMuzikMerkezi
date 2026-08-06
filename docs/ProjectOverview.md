# Project Overview

## Project Name

RORA Art Center - Student Tracking System

## Purpose

This project was developed to digitalize the student management processes of RORA Art Center.

The application allows administrators to:

- Register, edit and delete students, organised by instrument
- Track weekly lesson attendance
- Manage monthly payments, including the amount paid
- Define a default lesson fee per instrument
- Search students within an instrument or across all of them
- Generate monthly summary reports as `.txt` and as PDF, for any chosen month
- Store all data locally, with automatic and manual backups

## Scope

The system is designed for small and medium-sized music education centers. It runs on a single computer, with no server, database or internet connection.

## Technologies

- C#
- .NET Framework 4.7.2
- Windows Forms
- XML Serialization

No third-party libraries are used. PDF output is produced through the `Microsoft Print to PDF` printer built into Windows.

## Current Status

Current Version: v1.0

The application is in active development. The core feature set is complete and in use; current work covers interface refresh, high-DPI support and reporting depth. Open items are tracked as issues in GitLab and summarised in [SprintPlan.md](SprintPlan.md).
