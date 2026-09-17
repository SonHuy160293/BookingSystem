var builder = DistributedApplication.CreateBuilder(args);

var collectorEndpoint = builder.Configuration["Observability:Endpoint"] ?? "http://localhost:4317";

var sqlServerPassword = builder.AddParameter("sqlserver-password", secret: true);
var sqlServer = builder.AddSqlServer("sqlserver", sqlServerPassword).WithDataVolume();

var identityDb = sqlServer.AddDatabase("identitydb", "IdentityDb");
var identityMigration = builder.AddProject<Projects.BookingSystem_Identity_MigrationRunner>("identity-migration")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", collectorEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WithReference(identityDb, connectionName: "ApplicationDb")
    .WaitFor(sqlServer);
builder.AddProject<Projects.BookingSystem_Identity_API>("identity-api")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", collectorEndpoint)
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WithReference(identityDb, connectionName: "ApplicationDb")
    .WaitForCompletion(identityMigration)
    .WithHttpHealthCheck("/health");

//var orderDb = sqlServer.AddDatabase("orderdb", "OrderDb");
//var orderMigration = builder.AddProject<Projects.BookingSystem_Order_MigrationRunner>("order-migration")
//    .WithReference(orderDb, connectionName: "ApplicationDb")
//    .WaitFor(sqlServer);
//builder.AddProject<Projects.BookingSystem_Order_API>("order-api")
//    .WithReference(orderDb, connectionName: "ApplicationDb")
//    .WaitForCompletion(orderMigration)
//    .WithHttpHealthCheck("/health");

//var inventoryDb = sqlServer.AddDatabase("inventorydb", "InventoryDb");
//var inventoryMigration = builder.AddProject<Projects.BookingSystem_Inventory_MigrationRunner>("inventory-migration")
//    .WithReference(inventoryDb, connectionName: "ApplicationDb")
//    .WaitFor(sqlServer);
//builder.AddProject<Projects.BookingSystem_Inventory_API>("inventory-api")
//    .WithReference(inventoryDb, connectionName: "ApplicationDb")
//    .WaitForCompletion(inventoryMigration)
//    .WithHttpHealthCheck("/health");

//var paymentDb = sqlServer.AddDatabase("paymentdb", "PaymentDb");
//var paymentMigration = builder.AddProject<Projects.BookingSystem_Payment_MigrationRunner>("payment-migration")
//    .WithReference(paymentDb, connectionName: "ApplicationDb")
//    .WaitFor(sqlServer);
//builder.AddProject<Projects.BookingSystem_Payment_API>("payment-api")
//    .WithReference(paymentDb, connectionName: "ApplicationDb")
//    .WaitForCompletion(paymentMigration)
//    .WithHttpHealthCheck("/health");

//var cartDb = sqlServer.AddDatabase("cartdb", "CartDb");
//var cartMigration = builder.AddProject<Projects.BookingSystem_Cart_MigrationRunner>("cart-migration")
//    .WithReference(cartDb, connectionName: "ApplicationDb")
//    .WaitFor(sqlServer);
//builder.AddProject<Projects.BookingSystem_Cart_API>("cart-api")
//    .WithReference(cartDb, connectionName: "ApplicationDb")
//    .WaitForCompletion(cartMigration)
//    .WithHttpHealthCheck("/health");

builder.Build().Run();
