using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// User secrets hold the generated passwords for sql and the object store. The host adds them only in
// Development, so a Production profile starts with no value for any of them, generates fresh ones,
// and persists those back over the originals.
//
// That is not a harmless rotation. The database container is Persistent with a data volume, and
// Postgres only applies POSTGRES_PASSWORD when it initialises an empty data directory - so the volume
// keeps the password it was built with while every caller now presents a new one, and the local
// database is locked out with "28P01: password authentication failed for user postgres". GSBC.ImpactKids
// did exactly this on 2026-08-24; see its docs/modules/infrastructure/generated-passwords.md.
//
// Load them regardless of environment so every profile resolves the same parameters. In Development
// this is a second, identical source and changes nothing.
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

// EVERY fixed host port here must differ from GSBC.ImpactKids'. Both stacks are Persistent and both
// run at once on this laptop; ImpactKids holds 60535 (redis), 60536 (postgres), 60537 (S3), 63001
// (rabbit management) and 7263 (its YARP). The numbers below are recorded in .claude/app-local.md.
IResourceBuilder<PostgresServerResource> sql = builder.AddPostgres("sql")
    .WithHostPort(60546)
    .WithDataVolume("gsbc-accounting-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<PostgresDatabaseResource> db = sql.AddDatabase("accounting");

// SeaweedFS, S3-compatible, holds the receipts. This stack runs its OWN container on its own port and
// its own volume, locally AND in the cluster - a sharing arrangement with ImpactKids was considered and
// rejected on 2026-09-01: each instance is one small container, so sharing saves nothing worth the
// coupling, and separate identities mean a credential or capacity problem on one side cannot reach the
// other's objects. So the deployed configuration differs from this one only in the endpoint. Not MinIO:
// the community edition was archived in early 2026 and takes no security patches.
IResourceBuilder<ParameterResource> s3AccessKey =
    builder.AddParameter("s3-access-key", "gsbc-accounting", publishValueAsDefault: true);

// No special characters: this value is signed into S3 request headers and pasted into shell and YAML
// by hand often enough that a quoting mistake is the likelier failure than a short alphabet.
IResourceBuilder<ParameterResource> s3SecretKey = builder.AddResource(
    ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, "s3-secret-key", special: false));

IResourceBuilder<ContainerResource> s3 = builder.AddContainer("s3", "chrislusf/seaweedfs", "3.98")
    // Sized for THIS app's objects, not ImpactKids'. It stores 1-20 MB PDFs and phone photos of
    // receipts, where ImpactKids stores 30 KB JPEGs of faces - so its 128 MB x 8 volumes (a hard 1 GB
    // ceiling, deliberate there) would fill here, and the failure is a bare `400 InvalidRequest` on
    // PUT with the real cause ("No more free space left") only in the container log.
    //
    // -master.volumePreallocate=false IS carried across and is load-bearing. Left at its default,
    // `weed server` allocates volume files of 1 GB each and grows them seven at a time; ImpactKids
    // measured three small objects taking 7 GB of disk on 2026-08-29.
    .WithArgs("server", "-dir=/data", "-s3", "-s3.port=8333",
        "-master.volumeSizeLimitMB=1024", "-master.volumePreallocate=false", "-volume.max=30")
    .WithEnvironment("AWS_ACCESS_KEY_ID", s3AccessKey)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", s3SecretKey)
    .WithVolume("gsbc-accounting-s3-data", "/data")
    .WithHttpEndpoint(port: 60547, targetPort: 8333, name: "s3")
    // Persistent, but unlike Postgres it cannot lock you out: SeaweedFS reads its S3 identity from
    // AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY at every start and holds nothing about it on the
    // volume. The volume does hold real receipts under seven-year retention, so it is still not a
    // thing to delete casually.
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<ProjectResource> migrations =
    builder.AddProject<Projects.GSBC_Accounting_Workers_DbMigrations>("migrations")
        .WithReference(db)
        .WaitFor(db);

IResourceBuilder<ProjectResource> grpcService = builder.AddProject<Projects.GSBC_Accounting_Grpc>("grpc")
    .WithReference(db)
    .WithReference(migrations)
    .WaitForCompletion(migrations)
    // The gRPC service is the object store's only client. There is no ingress and no YARP route to
    // the store - a receipt reaches the browser through this service or not at all.
    .WithEnvironment("Attachments__ServiceUrl", s3.GetEndpoint("s3"))
    .WithEnvironment("Attachments__AccessKey", s3AccessKey)
    .WithEnvironment("Attachments__SecretKey", s3SecretKey)
    .WaitFor(s3);

IResourceBuilder<ProjectResource> wasm =
    builder.AddStandaloneBlazorWebAssemblyProject<Projects.GSBC_Accounting_WASM>("wasm");

IResourceBuilder<ProjectResource> yarp = builder.AddProject<Projects.GSBC_Accounting_YARP>("yarp")
    .WithReference(grpcService)
    .WaitFor(grpcService)
    .WithReference(wasm)
    .WaitFor(wasm)
    .WithExternalHttpEndpoints();

wasm.WithReference(yarp);
grpcService.WithReference(wasm);

builder.Build().Run();
