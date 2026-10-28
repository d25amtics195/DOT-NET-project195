// GlassCRM - ASP.NET Core minimal API. All data lives in memory (C# lists = temporary JSON data).
// Restarting the app resets everything back to the demo data below.
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseDefaultFiles();   // serves wwwroot/index.html
app.UseStaticFiles();

var gate = new object();
string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

// ---- in-memory "database" ----
var users = new List<User> { new("Demo Owner", "demo@shop.com", Hash("demo123"), "Demo Mart", "9876543210") };
var sessions = new Dictionary<string, string>();   // token -> email
var customers = new List<Customer>
{
    new(1, "Aarav Patel", "Wireless Earbuds", DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd")),
    new(2, "Diya Shah", "Smart Watch", DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd")),
    new(3, "Rohan Mehta", "Phone Case", DateTime.Today.AddDays(-5).ToString("yyyy-MM-dd")),
};
var products = new List<Product>
{
    new(1, "Wireless Earbuds", 1999, 25), new(2, "Smart Watch", 3499, 12), new(3, "Phone Case", 299, 80),
};
var employees = new List<Employee>
{
    new(1, "Neha Joshi", 28000), new(2, "Karan Desai", 22000), new(3, "Pooja Trivedi", 25000),
};

// ---- sign up / log in (open) ----
app.MapPost("/api/signup", (SignupReq r) =>
{
    if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Email) || string.IsNullOrWhiteSpace(r.Password))
        return Results.BadRequest(new { error = "Name, email and password are required." });
    if (r.Password.Length < 6)
        return Results.BadRequest(new { error = "Password must be at least 6 characters." });
    lock (gate)
    {
        if (users.Any(u => u.Email.Equals(r.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Results.Json(new { error = "That email is already registered." }, statusCode: 409);
        users.Add(new(r.Name.Trim(), r.Email.Trim(), Hash(r.Password), r.Shop?.Trim() ?? "", ""));
        return Results.Ok(new { message = "Account created." });
    }
});

app.MapPost("/api/login", (LoginReq r) =>
{
    lock (gate)
    {
        var u = users.FirstOrDefault(x => x.Email.Equals(r.Email?.Trim(), StringComparison.OrdinalIgnoreCase)
                                          && x.PasswordHash == Hash(r.Password ?? ""));
        if (u is null) return Results.Json(new { error = "Wrong email or password." }, statusCode: 401);
        var token = Guid.NewGuid().ToString("N");
        sessions[token] = u.Email;
        return Results.Ok(new { token });
    }
});

// ---- everything below needs a valid login token ----
string Token(HttpContext c) => c.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
var api = app.MapGroup("/api").AddEndpointFilter(async (ctx, next) =>
{
    bool ok; lock (gate) ok = sessions.ContainsKey(Token(ctx.HttpContext));
    if (!ok) return (object?)Results.Json(new { error = "Please log in." }, statusCode: 401);
    return await next(ctx);
});

api.MapGet("/me", (HttpContext c) =>
{
    lock (gate)
    {
        var u = users.First(x => x.Email == sessions[Token(c)]);
        return Results.Ok(new { u.Name, u.Email, u.Shop, u.Phone });
    }
});

api.MapPut("/me", (HttpContext c, ProfileReq r) =>
{
    if (string.IsNullOrWhiteSpace(r.Name)) return Results.BadRequest(new { error = "Name is required." });
    lock (gate)
    {
        var i = users.FindIndex(x => x.Email == sessions[Token(c)]);
        users[i] = users[i] with { Name = r.Name.Trim(), Shop = r.Shop?.Trim() ?? "", Phone = r.Phone?.Trim() ?? "" };
        return Results.Ok(new { users[i].Name, users[i].Email, users[i].Shop, users[i].Phone });
    }
});

Crud.Map(api, "/customers", customers, gate, x => x.Id, (x, id) => x with { Id = id },
    x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.Product));
Crud.Map(api, "/products", products, gate, x => x.Id, (x, id) => x with { Id = id },
    x => !string.IsNullOrWhiteSpace(x.Name) && x.Price >= 0 && x.Stock >= 0);
Crud.Map(api, "/employees", employees, gate, x => x.Id, (x, id) => x with { Id = id },
    x => !string.IsNullOrWhiteSpace(x.Name) && x.Salary >= 0);

app.Run();

// ---- data shapes ----
record User(string Name, string Email, string PasswordHash, string Shop, string Phone);
record Customer(int Id, string Name, string Product, string Date);
record Product(int Id, string Name, decimal Price, int Stock);
record Employee(int Id, string Name, decimal Salary);
record SignupReq(string Name, string Email, string Password, string? Shop);
record LoginReq(string Email, string Password);
record ProfileReq(string Name, string? Shop, string? Phone);

// Reusable list / add / delete endpoints for any record type
static class Crud
{
    public static void Map<T>(RouteGroupBuilder g, string path, List<T> list, object gate,
                              Func<T, int> getId, Func<T, int, T> withId, Func<T, bool> valid)
    {
        g.MapGet(path, () => { lock (gate) return Results.Ok(list.ToList()); });
        g.MapPost(path, (T item) =>
        {
            if (item is null || !valid(item)) return Results.BadRequest(new { error = "Please fill in all fields correctly." });
            lock (gate)
            {
                var saved = withId(item, list.Count == 0 ? 1 : list.Max(getId) + 1);
                list.Add(saved);
                return Results.Created($"/api{path}/{getId(saved)}", saved);
            }
        });
        g.MapDelete(path + "/{id:int}", (int id) =>
        {
            lock (gate) return list.RemoveAll(x => getId(x) == id) > 0 ? Results.NoContent() : Results.NotFound();
        });
    }
}
