# BlogPlatform

A small REST API for blog posts and comments, built with .NET 10, EF Core and SQLite.

## Run

You need the .NET 10 SDK. From the solution folder, run:

```bash
dotnet run --project BlogPlatform.Api --launch-profile http
```

Open [Scalar](http://localhost:5086/scalar) to try the endpoints.

The app creates the SQLite database and applies migrations on startup. Data is stored locally in `blogplatform.db`.

The database starts empty. Create a post, then use its ID to add comments or get its details. You can also use the requests in [BlogPlatform.Api.http](BlogPlatform.Api/BlogPlatform.Api.http). Update `@PostId` with the ID returned when creating a post.

## Tests

```bash
dotnet test
```

The integration tests use a separate temporary SQLite database for each test, with the same migrations as the app.

## Decisions

- SQLite keeps local setup simple, without an external database server.
- Controllers handle HTTP requests and responses. The service handles application logic and uses EF Core directly.
- Input validation returns `400`, and missing posts return `404`. Unexpected errors are logged and return a generic `500` response with a trace ID.

## Next steps

- Add pagination to the posts endpoint if the number of posts grows.
- Use Docker Compose to run the API with a database like SQL Server, making local setup easier.
- Review the test database setup as the suite grows, considering database reuse and cleanup between tests.
