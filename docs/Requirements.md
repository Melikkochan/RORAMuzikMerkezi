# Software Requirements

## Functional Requirements

### Student management

- Register a new student
- Edit a registered student's details
- Delete a student, with a confirmation step
- Validate the phone number format on entry
- Warn when a student with the same details is already registered
- Organise students into tabs by instrument
- Search students within a tab, and across all instruments at once
- Show one student's history: lessons and payments month by month, total collected, and which months are still unpaid

### Lesson tracking

- Mark attendance for each of the four weeks of a month
- Filter a tab by month and year

### Payment management

- Record and revert a monthly payment
- Store the amount paid, not only whether payment happened
- Define a default lesson fee per instrument, prefilled when recording a payment
- Remind the user of unpaid students after the 15th of the month

### Reporting

- Produce a monthly summary report as a `.txt` file on the desktop
- Produce the same report as a PDF, with the centre's logo
- Report income for the selected month
- Choose the reported period: current month, previous month, or any month and year
- Report a whole year or a free month range, showing income month by month and broken down by instrument, as both text and PDF

### Data storage

- Store all data locally in a single XML file
- Write the data file atomically, so an interrupted write cannot corrupt it
- Take an automatic backup on startup and keep the last 7 days
- Back up and restore manually from the menu
- Open the data folder from the menu
- Refuse to start a second instance, which would overwrite the first one's changes

## Non-Functional Requirements

- Easy-to-use interface
- Fast startup
- Local data storage
- Low hardware requirements
- Windows compatibility
- No server, database or internet connection
- No external libraries; only standard .NET Framework assemblies

## Target Users

- Music school administrators
- Secretaries
- Teachers
