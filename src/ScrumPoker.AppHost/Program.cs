using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder
        .AddMongoDB("mongo")
        .WithDataVolume();

var db = mongo.AddDatabase("ScrumPoker");

builder.AddProject<Projects.ScrumPoker_Web>("scrumpoker-web").WithReference(db).WaitFor(db);
builder.Build().Run();
