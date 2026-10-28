GLASSCRM - Customer Relationship Management website (ASP.NET Core + glass UI)

PAGES: Sign up, Log in, Dashboard, Customers, Products, Employees, Profile (sidebar layout)

STEP 1 - Install .NET (one time)
  Download the ".NET SDK" (version 8 or newer) from https://dotnet.microsoft.com/download
  Install it, then CLOSE and reopen your terminal.
  Check it worked:   dotnet --version

STEP 2 - Run the website
  1. Unzip GlassCRM.zip
  2. Open a terminal inside the GlassCRM folder
     (Windows tip: open the folder in File Explorer, click the address bar, type "cmd", press Enter)
  3. Run:   dotnet run      (first run needs internet for a few seconds)
  4. Your browser opens automatically. If not, go to  http://localhost:5000
  5. Log in with the demo account:   demo@shop.com  /  demo123
     (or create your own on the Sign up page)
  6. To stop the site: press Ctrl + C in the terminal.

NOTES
  - Data is temporary: it is stored in memory only (C# lists, sent to the page as JSON).
    Restarting the app resets everything to the demo data.
  - If port 5000 is busy, open Properties/launchSettings.json and change 5000 to 5050.
  - Design + JavaScript: wwwroot/index.html     Server code + API: Program.cs
