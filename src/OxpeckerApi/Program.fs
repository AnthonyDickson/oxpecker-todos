module OxpeckerApi.Program

open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.OpenApi
open Oxpecker
open Oxpecker.OpenApi
open OxpeckerApi.Auth
open OxpeckerApi.OpenApi
open OxpeckerApi.Todos
open Scalar.AspNetCore
open System.Collections.Generic
open System.Threading.Tasks

[<EntryPoint>]
let main (args : string array) : int =
    let builder = WebApplication.CreateBuilder args

    builder.Services
        .AddAuthentication(DemoScheme)
        .AddScheme<AuthenticationSchemeOptions, DemoBearerAuthHandler>(DemoScheme, ignore)
        .Services.AddAuthorization()
        .AddRouting()
        .AddOxpecker()
        .AddOpenApi (fun options ->
            options.AddSchemaTransformer<FSharpOptionSchemaTransformer> () |> ignore
            options.AddSchemaTransformer<FSharpRecordSchemaTransformer> () |> ignore
            options.AddSchemaTransformer<XmlDocSchemaTransformer> () |> ignore

            options.AddDocumentTransformer (fun doc _ _ ->
                if isNull doc.Components then
                    doc.Components <- OpenApiComponents ()

                if isNull doc.Components.SecuritySchemes then
                    doc.Components.SecuritySchemes <- Dictionary<string, IOpenApiSecurityScheme> ()

                doc.Components.SecuritySchemes["bearerAuth"] <-
                    OpenApiSecurityScheme (
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = $"Demo bearer token. Use `{DemoToken}`."
                    )

                Task.CompletedTask)
            |> ignore)
    |> ignore

    let app = builder.Build ()

    app.MapOpenApi () |> ignore

    app.MapScalarApiReference (fun opts ->
        opts
            .WithTitle("Oxpecker Todo API")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient (ScalarTarget.CSharp, ScalarClient.HttpClient)
        |> ignore)
    |> ignore

    let store = Store.start ()

    app.UseRouting () |> ignore
    app.UseAuthentication () |> ignore
    app.UseAuthorization () |> ignore
    app.UseOxpecker (Todos.Routes.endpoints store) |> ignore
    app.Run ()
    0
