CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110063450_InitialCreate') THEN
    CREATE TABLE "TelemetryRecords" (
        "Id" uuid NOT NULL,
        "DeviceId" character varying(50) NOT NULL,
        "Temperature" double precision NOT NULL,
        "Humidity" double precision NOT NULL,
        "Voltage" double precision NOT NULL,
        "Status" character varying(20) NOT NULL,
        "TimestampUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TelemetryRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110063450_InitialCreate') THEN
    CREATE INDEX "IX_TelemetryRecords_DeviceId_TimestampUtc" ON "TelemetryRecords" ("DeviceId", "TimestampUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261110063450_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261110063450_InitialCreate', '10.0.14');
    END IF;
END $EF$;
COMMIT;

